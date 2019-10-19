using Q.Lib.Extension;
using Q.Lib.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    public class SocketServer : BaseSocket
    {

        private BufferManager _BufferManager;
        private Socket _ListenSocket;            //监听Socket  
        private SocketEventPool _Pool;
        private int _ClientCount;              //连接的客户端数量  
        private Semaphore _MaxNumberAcceptedClients;
        private int _ClientIndex;
        private List<ServerClient> _Clients; //客户端列表  
        private const int _OpsToAlloc = 2;
        private int _MaxConnectNum = 200;    //最大连接数  
        private int _RevBufferSize = 1024;    //最大接收字节数  
        private ConcurrentDictionary<string, Action<ServerClient, SocketMsg>> _Commands = new ConcurrentDictionary<string, Action<ServerClient, SocketMsg>>();
        private ConcurrentDictionary<string, Action<AckItem>> _CallBacks = new ConcurrentDictionary<string, Action<AckItem>>();

        /// <summary>
        /// 异常回调
        /// </summary>
        public Action<Exception> Error;
        /// <summary>
        /// 完成启动回调
        /// </summary>
        public Action StartOver;
        /// <summary>
        /// 新客户端连接
        /// </summary>
        public Action<ServerClient> NewClient;
        /// <summary>
        /// 终端注册
        /// </summary>
        public Action<ServerClient> RegistClient;
        /// <summary>
        /// 客户端移除
        /// </summary>
        public Action<ServerClient> RemoveClient;
        /// <summary>
        /// 交互日志
        /// </summary>
        public Action<string, string> Log;

        /// <summary>  
        /// 获取客户端列表  
        /// </summary>  
        public List<ServerClient> ClientList { get { return _Clients; } }


        /// <summary>  
        /// 构造函数  
        /// </summary>  
        /// <param name="numConnections">最大连接数</param>  
        /// <param name="receiveBufferSize">缓存区大小</param>  
        public SocketServer()
        {
            _ClientCount = 0;
            _BufferManager = new BufferManager(_RevBufferSize * _MaxConnectNum * _OpsToAlloc, _RevBufferSize);
            _Pool = new SocketEventPool(_MaxConnectNum);
            _MaxNumberAcceptedClients = new Semaphore(_MaxConnectNum, _MaxConnectNum);
            _BufferManager.InitBuffer();
            _Clients = new List<ServerClient>();
            SocketAsyncEventArgs readWriteEventArg;
            for (int i = 0; i < _MaxConnectNum; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new ServerClient();
                _BufferManager.SetBuffer(readWriteEventArg);
                _Pool.Push(readWriteEventArg);
            }
        }

        /// <summary>  
        /// 启动服务  
        /// </summary>  
        public bool Start(int port)
        {
            try
            {
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
                _Clients.Clear();
                _ListenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _ListenSocket.Bind(localEndPoint);
                _ListenSocket.Listen(_MaxConnectNum);
                _Commands.TryAdd("Sys_Regist", _RegistClient);// 系统客户端注册
                StartAccept(null);
                StartOver?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                return false;
            }
        }

        private void _RegistClient(ServerClient client, SocketMsg msg)
        {
            string clientName = msg.Data?.ClientName;
            if (!string.IsNullOrEmpty(clientName))
            {
                client.ClientName = clientName;
                client.CallBack(msg.CallBackCommand, new AckItem());
                RegistClient?.Invoke(client);
                QCrontab.CancleTask(client.CrontabTaskID);
            }
            else
            {
                client.CallBack(msg.CallBackCommand, new AckItem(-1, "字段 ClientName 缺失"));
            }

        }

        /// <summary>  
        /// 停止服务  
        /// </summary>  
        public void Stop()
        {
            foreach (ServerClient client in _Clients)
            {
                try
                {
                    client.Socket.Shutdown(SocketShutdown.Both);
                    RemoveClient?.Invoke(client);
                }
                catch (Exception) { }
            }
            try
            {
                _ListenSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }

            _ListenSocket.Close();
            int c_count = _Clients.Count;
            lock (_Clients) { _Clients.Clear(); }



        }
        public bool RegistAction(string actionKey, Action<ServerClient, SocketMsg> action)
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

        public void CloseClient(ServerClient client)
        {
            try
            {
                client.Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }
        }



        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += (s, e) => { ProcessAccept(e); };
            }
            else
            {
                // socket must be cleared since the context object is being reused  
                acceptEventArg.AcceptSocket = null;
            }

            _MaxNumberAcceptedClients.WaitOne();
            if (!_ListenSocket.AcceptAsync(acceptEventArg))
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                Interlocked.Increment(ref _ClientCount);
                Interlocked.Increment(ref _ClientIndex);

                SocketAsyncEventArgs readEventArgs = _Pool.Pop();
                ServerClient client = (ServerClient)readEventArgs.UserToken;
                client.Socket = e.AcceptSocket;
                client.ConnectTime = DateTime.Now;
                client.Remote = e.AcceptSocket.RemoteEndPoint;
                client.IPAddress = ((IPEndPoint)(e.AcceptSocket.RemoteEndPoint)).Address;
                client.Index = _ClientIndex;
                client.Log = Log;
                lock (_Clients) { _Clients.Add(client); }


                NewClient?.Invoke(client);
                client.CrontabTaskID = QCrontab.RunOnceWithTime(DateTime.Now.AddSeconds(20), () =>
                {
                    client.CrontabTaskID = null;
                    if (string.IsNullOrEmpty(client.ClientName))
                    {
                        QLog.SendLog($"[终端{client.Index}]{client.Remote.ToString()} 20s未注册，剔除");
                        CloseClient(client);
                    }

                }, $"[终端{_ClientIndex}]注册检查");

                if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
                {
                    ProcessReceive(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }

            // Accept the next connection request  
            if (e.SocketError == SocketError.OperationAborted) return;
            StartAccept(e);
        }


        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler  
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    Error?.Invoke(new ArgumentException("The last operation completed on the socket was not a receive or send"));
                    break;
            }

        }



        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                // check if the remote host closed the connection  
                ServerClient client = (ServerClient)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //读取数据  
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);//从e.Buffer块中复制数据出来，保证它可重用
                    lock (client.Buffer)
                    {
                        client.Buffer.AddRange(data);
                    }

                    do
                    {
                        byte[] lenBytes = client.Buffer.GetRange(0, 8).ToArray();
                        string lengthStr = Encoding.UTF8.GetString(lenBytes, 0, lenBytes.Length);
                        if (int.TryParse(lengthStr, NumberStyles.HexNumber, null, out var packageLen))
                        {
                            if (packageLen > client.Buffer.Count)
                            {   //长度不够时,退出循环,让程序继续接收  
                                break;
                            }
                            byte[] rev = client.Buffer.GetRange(8, packageLen - 8).ToArray();

                            //移除
                            lock (client.Buffer)
                            {
                                client.Buffer.RemoveRange(0, packageLen);
                            }

                            string msgStr = Encoding.UTF8.GetString(rev);
                            if (Log != null)
                            {
                                Task.Run(() => { 
                                    
                                    if (msgStr.StartsWith("Ping")){ 
                                        Log.Invoke("Receive_Ping", $"[{client.ClientName}]{msgStr}");
                                    }
                                    else
                                    {
                                        Log.Invoke("Receive", $"[{client.ClientName}]{msgStr}");
                                    }
                                });
                            }
                            Task.Run(() =>
                            {
                                try
                                {
                                    if (msgStr.StartsWith("Ping"))
                                    {
                                        //保活消息
                                        SendStr(client, "Pong");
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
                                                _Commands[socketMsg.Command].Invoke(client, socketMsg);
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
                        else
                        {
                            lock (client.Buffer)
                            {
                                //头部未包含长度信息，丢掉
                                client.Buffer.Clear();
                                break;
                            }
                        }

                    } while (client.Buffer.Count > 8);

                    //继续接收. 为什么要这么写,请看Socket.ReceiveAsync方法的说明  
                    if (!client.Socket.ReceiveAsync(e))
                        this.ProcessReceive(e);
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }


        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ServerClient client = (ServerClient)e.UserToken;
                bool willRaiseEvent = client.Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        //关闭客户端  
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            ServerClient client = e.UserToken as ServerClient;

            lock (_Clients) { _Clients.Remove(client); }
            //如果有事件,则调用事件,发送客户端数量变化通知  
            RemoveClient?.Invoke(client);
            try
            {
                client.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            client.Socket.Close();
            Interlocked.Decrement(ref _ClientCount);
            _MaxNumberAcceptedClients.Release();
            e.UserToken = new ServerClient();
            _Pool.Push(e);
        }

        private void SendStr(ServerClient client, string str)
        {
            if (client == null || client.Socket == null || !client.Socket.Connected)
                return;
            try
            {
                var data = WriteStream(str);
                SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                sendArg.UserToken = client;
                sendArg.SetBuffer(data, 0, data.Length);
                client.Socket.SendAsync(sendArg);
                if (Log != null)
                {
                    if (str == "Pong")
                    {
                        Task.Run(() => { Log.Invoke("Send_Pong", str); });
                    }
                    else
                    {
                        Task.Run(() => { Log.Invoke("Send", str); });
                    }
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }

        /// <summary>
        /// 异步的发送数据
        /// </summary>
        public void SendAsync(string clientName, string command, object param, Action<AckItem> callback = null)
        {
            var client = _Clients.FirstOrDefault(x => x.ClientName == clientName);
            if (client != null)
            {
                SendAsync(client, command, param, callback);
            }
        }


        /// <summary>
        /// 异步的发送数据
        /// </summary>
        public void SendAsync(ServerClient client, string command, object param, Action<AckItem> callback = null)
        {
            if (client == null || client.Socket == null || !client.Socket.Connected)
                return;
            try
            {
                string callBackCommand = null;
                if (callback != null)
                {
                    callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";
                    _CallBacks.TryAdd(callBackCommand, callback);
                }
                var datastr = Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand });
                var data = WriteStream(datastr);
                SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                sendArg.UserToken = client;
                sendArg.SetBuffer(data, 0, data.Length);
                client.Socket.SendAsync(sendArg);
                if (Log != null)
                {
                    Task.Run(() => { Log.Invoke("Send", $"[{client.ClientName}]{datastr}"); });
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }

        public AckItem SendSync(string clientName, string command, object param, int timeOut = 30)
        {
            var client = _Clients.FirstOrDefault(x => x.ClientName == clientName);
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
            try
            {
                AckItem ack = new AckItem(-1, "请求超时");
                if (client == null || client.Socket == null || !client.Socket.Connected)
                    return new AckItem(-1, "终端状态错误");
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                string callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";

                var datastr = Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand });
                var data = WriteStream(datastr);
                _CallBacks.TryAdd(callBackCommand, (a) =>
                {
                    ack = a;
                    resetEvent.Set();
                });

                SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                sendArg.UserToken = client;
                sendArg.SetBuffer(data, 0, data.Length);
                client.Socket.SendAsync(sendArg);
                if (Log != null)
                {
                    Task.Run(() => { Log.Invoke("Send", $"[{client.ClientName}]{datastr}"); });
                }

                resetEvent.WaitOne(TimeSpan.FromSeconds(timeOut));
                return ack;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                return new AckItem(-1, ex.Message);
            }
        }

    }
}
