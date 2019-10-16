using Q.Lib.Extension;
using Q.Lib.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    public class SocketServer : IDisposable
    {
        private System.Net.Sockets.Socket _ServerSocket;
        private int _CurrentClientCount;
        private int _BuffrSize;
        private Semaphore _MaxAcceptedClients;
        private BufferManager _bufferManager;
        private List<SocketServerClient> _Clients = new List<SocketServerClient>();
        private bool disposed;
        public bool IsRunning { get; private set; }

        private ConcurrentDictionary<string, Action<SocketServerClient, SocketMsg>> _Commands = new ConcurrentDictionary<string, Action<SocketServerClient, SocketMsg>>();

        private ConcurrentDictionary<string, Action<AckItem>> _CallBacks = new ConcurrentDictionary<string, Action<AckItem>>();

        //启动异常回调
        public Action<Exception> Error;
        //完成启动回调
        public Action StartOver;
        //新客户端连接
        public Action<SocketServerClient> NewClient;

        #region 启动
        public bool Start(int port, int maxClient = 200)
        {
            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
                _ServerSocket = new System.Net.Sockets.Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _ServerSocket.Bind(localEndPoint);
                _ServerSocket.Listen(maxClient);
                StartAccept(null);
                StartOver?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion

        #region 接受客户端连接

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)  //初始化用
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted); //回调函数 实例化
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }
            bool bl = _ServerSocket.AcceptAsync(acceptEventArg); //该异步方法 每次只能接收一个连接。需要循环调用

            if (!bl)
            {
                this.ProcessAccept(acceptEventArg);
            }
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                System.Net.Sockets.Socket socket = e.AcceptSocket;//和客户端关联的socket
                if (socket.Connected)
                {
                    try
                    {
                        Interlocked.Increment(ref _CurrentClientCount);//原子操作加1
                        var client = new SocketServerClient();
                        client.Socket = socket;
                        client.SocketAsyncEventArgs = e;
                        NewClient?.Invoke(client);

                        if (!socket.ReceiveAsync(e))//投递接收请求
                        {
                            ProcessReceive(client);
                        }
                    }
                    catch (SocketException ex)
                    {
                        Error?.Invoke(ex);
                    }
                    //投递下一个接受请求
                    StartAccept(e);
                }
            }
        }
        #endregion

        #region 数据接收
        private void ProcessReceive(SocketServerClient e)
        {

            if (e.SocketAsyncEventArgs.SocketError == SocketError.Success)//if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // 检查远程主机是否关闭连接
                if (e.SocketAsyncEventArgs.BytesTransferred > 0)
                {
                    //判断所有需接收的数据是否已经完成
                    if (e.Socket.Available == 0)
                    {
                        byte[] data = new byte[e.SocketAsyncEventArgs.BytesTransferred];
                        Array.Copy(e.SocketAsyncEventArgs.Buffer, e.SocketAsyncEventArgs.Offset, data, 0, data.Length);//从e.Buffer块中复制数据出来，保证它可重用

                        string msgStr = Encoding.UTF8.GetString(data);
                        Task.Run(() =>
                        {
                            try
                            {
                                if (msgStr.StartsWith("Ping"))
                                {
                                    //保活消息
                                    byte[] res = Encoding.UTF8.GetBytes("Pong");
                                    e.Socket.Send(res, res.Length, SocketFlags.None);
                                }
                                else if (msgStr.StartsWith("Pong"))
                                {
                                    //保活消息 丢弃
                                }
                                else if (msgStr.StartsWith("{"))
                                {
                                    SocketMsg socketMsg = Json.ToObj<SocketMsg>(msgStr);

                                    if (!string.IsNullOrEmpty(socketMsg.Command))
                                    {
                                        if (socketMsg.Command.StartsWith("CallBack_"))
                                        {
                                            //移除并取出调用
                                            if (_CallBacks.TryRemove(socketMsg.Command, out var commandAction))
                                            {
                                                commandAction.Invoke(Json.Convert2T<AckItem>(socketMsg.Data));
                                            }
                                            else
                                            {
                                                QLog.SendLog_Debug("回调命令:" + socketMsg.Command + "不存在");
                                            }
                                        }
                                        else if (_Commands.ContainsKey(socketMsg.Command))
                                        {
                                            _Commands[socketMsg.Command].Invoke(e, socketMsg);
                                        }
                                        else
                                        {
                                            QLog.SendLog_Debug("未识别命令:" + socketMsg.Command);
                                        }
                                    }
                                    else
                                    {
                                        QLog.SendLog_Debug("未找到命令:" + msgStr);
                                    }
                                }
                                else
                                {
                                    QLog.SendLog_Debug("未识别消息:" + msgStr);
                                }
                            }
                            catch (Exception ex)
                            {
                                Error?.Invoke(ex);
                            }
                        });

                    }

                    if (!e.Socket.ReceiveAsync(e.SocketAsyncEventArgs))//为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        //同步接收时处理接收完成事件
                        ProcessReceive(e);
                    }
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        #endregion

        #region 发送数据


        public void Send(string name, string command, object param, Action<string> SendOver, Action<AckItem> callback)
        {
            var client = _Clients.FirstOrDefault(x => x.ClientName == name);
            if (client != null)
            {
                Send(client, command, param, SendOver, callback);
            }
            else
            {
                SendOver.Invoke("Error:未找到终端");
            }
        }


        /// <summary>
        /// 异步的发送数据
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        public void Send(SocketServerClient client, string command, object param, Action<string> SendOver, Action<AckItem> callback)
        {
            if (client.SocketAsyncEventArgs.SocketError == SocketError.Success)
            {

                if (client.Socket.Connected)
                {
                    string callBackCommand = null;
                    if (callback != null)
                    {
                        callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";
                    }
                    var data = Encoding.UTF8.GetBytes(Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand }));
                    Array.Copy(data, 0, client.SocketAsyncEventArgs.Buffer, 0, data.Length);//设置发送数据
                    _CallBacks.TryAdd(callBackCommand, callback);

                    if (!client.Socket.SendAsync(client.SocketAsyncEventArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        SendOver?.Invoke("OK");
                    }
                    else
                    {
                        CloseClientSocket(client);
                    }
                }
            }
        }
        #endregion


        #region 停止服务

        /// <summary>
        /// 停止服务
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                _ServerSocket.Close();
                //TODO 关闭对所有客户端的连接

            }
        }

        #endregion

        #region 关闭连接
        private void CloseClientSocket(SocketServerClient e)
        {

            CloseClientSocket(e.Socket, e.SocketAsyncEventArgs);
            lock (_Clients)
            {
                _Clients.Remove(e);
            }
        }

        private void CloseClientSocket(System.Net.Sockets.Socket socket, SocketAsyncEventArgs e)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
            finally
            {
                socket.Close();
            }
            Interlocked.Decrement(ref _CurrentClientCount);
        }
        #endregion

        #region 资源释放

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Stop();
                        if (_ServerSocket != null)
                        {
                            _ServerSocket = null;
                        }
                    }
                    catch (SocketException ex)
                    {
                        //TODO 事件
                    }
                }
                disposed = true;
            }
        }
        #endregion
    }
}
