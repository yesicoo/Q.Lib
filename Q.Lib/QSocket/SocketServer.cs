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
    public class SocketServer : BaseSocket, IDisposable
    {
        private System.Net.Sockets.Socket _ServerSocket;
        private int _CurrentClientCount;
        private List<ServerClient> _Clients = new List<ServerClient>();
        private BufferManager _BufferManager;
        private bool disposed;
        private Semaphore _maxAcceptedClients;
        public bool IsRunning { get; private set; }

        private ConcurrentDictionary<string, Action<ServerClient, SocketMsg>> _Commands = new ConcurrentDictionary<string, Action<ServerClient, SocketMsg>>();

        private ConcurrentDictionary<string, Action<AckItem>> _CallBacks = new ConcurrentDictionary<string, Action<AckItem>>();

        //启动异常回调
        public Action<Exception> Error;
        //完成启动回调
        public Action StartOver;
        //新客户端连接
        public Action<ServerClient> NewClient;

        public Action<ServerClient> RegistClient;

        //客户端移除
        public Action<ServerClient> RemoveClient;


        #region 启动
        public bool Start(int port, int maxClient = 200)
        {
            try
            {
                _BufferManager = new BufferManager(1024 * maxClient * 2, 1024);
                _maxAcceptedClients = new Semaphore(maxClient, maxClient);
                _BufferManager.InitBuffer();
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
                _ServerSocket = new System.Net.Sockets.Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _ServerSocket.Bind(localEndPoint);
                _ServerSocket.Listen(maxClient);
                _Commands.TryAdd("Sys_Regist", _RegistClient);// 系统客户端注册
                StartAccept(null);
                StartOver?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                QLog.SendLog_Exception(ex.ToString());
                return false;
            }
        }
        #endregion

        void _RegistClient(ServerClient client, SocketMsg msg)
        {
            string clientName = msg.Data?.ClientName;
            if (!string.IsNullOrEmpty(clientName))
            {
                client.ClientName = clientName;
                client.CallBack(msg.CallBackCommand, new AckItem());
                RegistClient?.Invoke(client);
            }
            else
            {
                client.CallBack(msg.CallBackCommand, new AckItem(-1, "字段 ClientName 缺失"));
            }
        }


        public bool RegisterAction(string actionKey, Action<ServerClient, SocketMsg> action)
        {
            if (string.IsNullOrEmpty(actionKey) || action == null)
            {
                QLog.SendLog($"命令[{actionKey}] 或这执行方法无效");
                return false;
            }

            if (_Commands.ContainsKey(actionKey))
            {
                QLog.SendLog($"命令[{actionKey}] 已存在，请勿重复添加");
                return false;
            }
            else
            {
                return _Commands.TryAdd(actionKey, action);

            }
        }


        #region 接受客户端连接

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)  //初始化用
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += (s, e) => { ProcessAccept(e); };
                _BufferManager.SetBuffer(acceptEventArg);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            _maxAcceptedClients.WaitOne();
            if (!_ServerSocket.AcceptAsync(acceptEventArg))
            {
                this.ProcessAccept(acceptEventArg);

                //如果I/O挂起等待异步则触发AcceptAsyn_Asyn_Completed事件
                //此时I/O操作同步完成，不会触发Asyn_Completed事件，所以指定BeginAccept()方法
            }
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
                        var client = new ServerClient();
                        client.Socket = socket;
                        client.SocketAsyncEventArgs = e;
                        NewClient?.Invoke(client);

                        if (!socket.ReceiveAsync(e))//投递接收请求
                        {
                            ProcessReceive(client);
                        }

                        QCrontab.RunOnceWithTime(DateTime.Now.AddSeconds(20), () =>
                        {

                            if (string.IsNullOrEmpty(client.ClientName))
                            {
                                QLog.SendLog($"终端{socket.RemoteEndPoint.ToString()} 20s未注册，剔除");
                                CloseClientSocket(client);
                            }

                        }, $"终端{socket.RemoteEndPoint.ToString()}注册检查");
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
        private void ProcessReceive(ServerClient e)
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

                        string msgStr = Encoding.UTF8.GetString(data.Skip(8).ToArray());
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


        public void SendAsync(string name, string command, object param, Action<AckItem> callback = null)
        {
            var client = _Clients.FirstOrDefault(x => x.ClientName == name);
            if (client != null)
            {
                SendAsync(client, command, param, callback);
            }
        }


        /// <summary>
        /// 异步的发送数据
        /// </summary>
        /// <param name="e"></param>
        /// <param name="data"></param>
        public void SendAsync(ServerClient client, string command, object param, Action<AckItem> callback = null)
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
                    var data = WriteStream(Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand }));
                    Array.Copy(data, 0, client.SocketAsyncEventArgs.Buffer, 0, data.Length);//设置发送数据
                    _CallBacks.TryAdd(callBackCommand, callback);

                    if (!client.Socket.SendAsync(client.SocketAsyncEventArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        CloseClientSocket(client);
                    }
                }
            }
        }

        public AckItem SendSync(string name, string command, object param, int timeOut = 30)
        {
            var client = _Clients.FirstOrDefault(x => x.ClientName == name);
            if (client != null)
            {
                return SendSync(client, command, param, timeOut);
            }
            else
            {
                return new AckItem(-1, "终端不存在");
            }
        }

        /// <summary>
        /// 同步接收返回结果
        /// </summary>
        /// <param name="client"></param>
        /// <param name="command"></param>
        /// <param name="param"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public AckItem SendSync(ServerClient client, string command, object param, int timeOut = 30)
        {
            AckItem ack = new AckItem(-1, "请求超时");
            if (client.SocketAsyncEventArgs.SocketError == SocketError.Success)
            {

                if (client.Socket.Connected)
                {
                    ManualResetEvent resetEvent = new ManualResetEvent(false);
                    string callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";

                    var data = WriteStream(Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand }));
                    Array.Copy(data, 0, client.SocketAsyncEventArgs.Buffer, 0, data.Length);//设置发送数据
                    _CallBacks.TryAdd(callBackCommand, (a) =>
                    {
                        ack = a;
                        resetEvent.Set();
                    });

                    if (!client.Socket.SendAsync(client.SocketAsyncEventArgs))//投递发送请求，这个函数有可能同步发送出去，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        CloseClientSocket(client);
                        resetEvent.Set();
                        return new AckItem(-1, "发送失败");
                    }
                    resetEvent.WaitOne(TimeSpan.FromSeconds(timeOut));
                    return ack;
                }
                else
                {
                    return new AckItem(-1, "终端状态错误");
                }
            }
            else
            {
                return new AckItem(-1, "终端状态错误");
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
        private void CloseClientSocket(ServerClient c)
        {

            CloseClientSocket(c.Socket, c.SocketAsyncEventArgs);
            lock (_Clients)
            {
                _Clients.Remove(c);
                RemoveClient?.Invoke(c);
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
            _maxAcceptedClients.Release();
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
