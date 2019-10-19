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
    public class SocketClient : BaseSocket, IDisposable
    {


        private Socket _ClientSocket;
        private IPEndPoint _HostEndPoint;

        private BufferManager _BufferManager;
        public string _ClientName;
        public List<byte> _Buffer;
        public int _TagCount = 0;
        private const int _BuffSize = 1024;
        private bool _Connected = false;
        private bool _Keeplive = false;
        private bool _Reconnect = false;
        private static AutoResetEvent _AutoConnectEvent = new AutoResetEvent(false);
        private List<QSocketEventArgs> _ListArgs = new List<QSocketEventArgs>();
        private QSocketEventArgs _ReceiveEventArgs = new QSocketEventArgs();
        private ConcurrentDictionary<string, Action<SocketClient, SocketMsg>> _Commands = new ConcurrentDictionary<string, Action<SocketClient, SocketMsg>>();
        private ConcurrentDictionary<string, Action<AckItem>> _CallBacks = new ConcurrentDictionary<string, Action<AckItem>>();

        //对外委托方法
        public Action<Exception> Error;
        public Action<SocketClient> Closed;
        public Action<SocketClient> Registed;
        public Action<string, string> Log;

        public string _keepliveCode="";

        /// <summary>  
        /// 当前连接状态  
        /// </summary>  
        public bool Connected { get { return _ClientSocket != null && _ClientSocket.Connected; } }

        //初始化
        public SocketClient(string ip, int port, bool keeplive = true, bool reconnect = true)
        {
            // Instantiates the endpoint and socket.  
            _HostEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            _BufferManager = new BufferManager(_BuffSize * 2, _BuffSize);
            _Buffer = new List<byte>();
            _Keeplive = keeplive;
            _Reconnect = reconnect;
        }

        /// <summary>  
        /// 连接到主机  
        /// </summary>  
        /// <returns>0.连接成功, 其他值失败,参考SocketError的值列表</returns>  
        public SocketError Connect(string clientName)
        {
            _ClientName = clientName;
            _ClientSocket = new Socket(_HostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.UserToken = _ClientSocket;
            connectArgs.RemoteEndPoint = _HostEndPoint;
            connectArgs.Completed += OnConnect;
            _ClientSocket.ConnectAsync(connectArgs);
            _AutoConnectEvent.WaitOne();
            RegistClient(clientName);
            if (_Keeplive)
            {
                QLog.SendLog_Debug("Socket终端保活启动");
                Task.Run(() =>
                {
                    while (_Connected)
                    {
                        lock (_keepliveCode)
                        {
                            if (_keepliveCode == "")
                            {
                                _keepliveCode = QTools.RandomCode(5);
                                SendStr("Ping" + _keepliveCode);
                            }
                            else
                            {
                                Disconnect();
                                break;
                            }
                        }
                        Thread.Sleep(5000);
                    }
                });
            }

            return connectArgs.SocketError;
        }

        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            _AutoConnectEvent.Set(); //释放阻塞.  
            _Connected = (e.SocketError == SocketError.Success);
            if (_Connected)
            {
                _BufferManager.InitBuffer();
                //发送参数  
                InitSendArgs();
                //接收参数  
                _ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                _ReceiveEventArgs.UserToken = e.UserToken;
                _ReceiveEventArgs.ArgsTag = 0;
                _BufferManager.SetBuffer(_ReceiveEventArgs);

                //启动接收,不管有没有,一定得启动.否则有数据来了也不知道.  
                if (!e.ConnectSocket.ReceiveAsync(_ReceiveEventArgs))
                    ProcessReceive(_ReceiveEventArgs);
            }
            else
            {
                Disconnect();
            }

        }

        private QSocketEventArgs InitSendArgs()
        {
            QSocketEventArgs sendArg = new QSocketEventArgs();
            sendArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            sendArg.UserToken = _ClientSocket;
            sendArg.RemoteEndPoint = _HostEndPoint;
            sendArg.IsUsing = false;
            Interlocked.Increment(ref _TagCount);
            sendArg.ArgsTag = _TagCount;
            lock (_ListArgs)
            {
                _ListArgs.Add(sendArg);
            }
            return sendArg;
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            QSocketEventArgs mys = (QSocketEventArgs)e;
            // determine which type of operation just completed and call the associated handler  
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    mys.IsUsing = false; //数据发送已完成.状态设为False  
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                // check if the remote host closed the connection  
                System.Net.Sockets.Socket token = (System.Net.Sockets.Socket)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, data.Length);//从e.Buffer块中复制数据出来，保证它可重用
                    lock (_Buffer)
                    {
                        _Buffer.AddRange(data);
                    }


                    do
                    {
                        byte[] lenBytes = _Buffer.GetRange(0, 8).ToArray();
                        string lengthStr = Encoding.UTF8.GetString(lenBytes, 0, lenBytes.Length);
                        if (int.TryParse(lengthStr, NumberStyles.HexNumber, null, out var packageLen))
                        {
                            if (packageLen > _Buffer.Count)
                            {   //长度不够时,退出循环,让程序继续接收  
                                break;
                            }
                            byte[] rev = _Buffer.GetRange(8, packageLen - 8).ToArray();

                            //移除
                            lock (_Buffer)
                            {
                                _Buffer.RemoveRange(0, packageLen);
                            }

                            string msgStr = Encoding.UTF8.GetString(rev);
                            if (Log != null)
                            {
                                Task.Run(() =>
                                {

                                    if (msgStr.StartsWith("Ping"))
                                    {
                                        Log.Invoke("Receive_Ping", msgStr);
                                    }
                                    else if (msgStr.StartsWith("Pong"))
                                    {
                                        Log.Invoke("Receive_Pong", msgStr);
                                    }
                                    else
                                    {
                                        Log.Invoke("Receive", msgStr);
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
                                        SendStr("Pong"+ msgStr.Substring(4));
                                    }
                                    else if (msgStr.StartsWith("Pong"))
                                    {
                                        string pongCode = msgStr.Substring(4);
                                        lock (_keepliveCode)
                                        {
                                            if (_keepliveCode == pongCode)
                                            {
                                                _keepliveCode = "";
                                            }
                                        }
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
                                                _Commands[socketMsg.Command].Invoke(this, socketMsg);
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
                            lock (_Buffer)
                            {
                                //头部未包含长度信息，丢掉
                                _Buffer.Clear();
                                break;
                            }
                        }

                    } while (_Buffer.Count > 8);

                    if (!token.ReceiveAsync(e))//为接收下一段数据，投递接收请求，这个函数有可能同步完成，这时返回false，并且不会引发SocketAsyncEventArgs.Completed事件
                    {
                        //同步接收时处理接收完成事件
                        ProcessReceive(e);
                    }
                }
                else
                {
                    Disconnect();
                }
            }
            catch (Exception ex)
            {
                Error.Invoke(ex);
            }
        }

        private void SendStr(string str)
        {
            var sendData = WriteStream(str);
            QSocketEventArgs sendArgs = _ListArgs.Find(a => a.IsUsing == false);

            if (sendArgs == null)
            {
                sendArgs = InitSendArgs();
            }
            lock (sendArgs)
            {
                sendArgs.IsUsing = true;
                sendArgs.SetBuffer(sendData, 0, sendData.Length);
            }
            _ClientSocket.SendAsync(sendArgs);
            if (Log != null)
            {
                if (str.StartsWith("Ping"))
                {
                    Task.Run(() => { Log.Invoke("Send_Ping", str); });
                }
                else if (str.StartsWith("Pong"))
                {
                    Task.Run(() => { Log.Invoke("Send_Pong", str); });
                }
                else
                {
                    Task.Run(() => { Log.Invoke("Send", str); });
                }
            }
        }


        public void SendAsync(string command, object param, Action<AckItem> callBack = null)
        {
            try
            {
                if (_Connected)
                {
                    string callBackCommand = null;
                    if (callBack != null)
                    {
                        callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";
                        _CallBacks.TryAdd(callBackCommand, callBack);
                    }

                    var sendStr = Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand });
                    var sendData = WriteStream(sendStr);

                    QSocketEventArgs sendArgs = _ListArgs.Find(a => a.IsUsing == false);

                    if (sendArgs == null)
                    {
                        sendArgs = InitSendArgs();
                    }
                    lock (sendArgs)
                    {
                        sendArgs.IsUsing = true;
                        sendArgs.SetBuffer(sendData, 0, sendData.Length);

                    }
                    _ClientSocket.SendAsync(sendArgs);
                    if (Log != null)
                    {
                        Task.Run(() => { Log.Invoke("Send", sendStr); });
                    }
                }
                else
                {
                    Error?.Invoke(new SocketException((int)SocketError.NotConnected));
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
            }
        }

        public AckItem SendSync(string command, object param, int timeOut = 30)
        {
            try
            {
                AckItem ack = new AckItem(-1, "客户端未连接");
                if (_Connected)
                {
                    ack = new AckItem(-1, "请求超时");
                    ManualResetEvent resetEvent = new ManualResetEvent(false);
                    string callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";
                    var sendStr = Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand });
                    var sendData = WriteStream(sendStr);
                    QSocketEventArgs sendArgs = _ListArgs.Find(a => a.IsUsing == false);

                    if (sendArgs == null)
                    {
                        sendArgs = InitSendArgs();
                    }
                    lock (sendArgs)
                    {
                        sendArgs.IsUsing = true;
                        sendArgs.SetBuffer(sendData, 0, sendData.Length);
                    }
                    _CallBacks.TryAdd(callBackCommand, (r) =>
                    {

                        ack = r;
                        resetEvent.Set();
                    });
                    _ClientSocket.SendAsync(sendArgs);
                    if (Log != null)
                    {
                        Task.Run(() => { Log.Invoke("Send", sendStr); });
                    }
                    resetEvent.WaitOne(TimeSpan.FromSeconds(timeOut));
                    return ack;
                }
                else
                {
                    Error?.Invoke(new SocketException((int)SocketError.NotConnected));
                }
                return ack;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                return new AckItem(-1, ex.Message);
            }
        }


        private void RegistClient(string clientName)
        {
            var ack = SendSync("Sys_Regist", new { ClientName = clientName });
            if (ack.ResCode == 0)
            {
                Registed?.Invoke(this);
            }
        }
        public bool RegistAction(string actionKey, Action<SocketClient, SocketMsg> action)
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

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                ProcessError(e);
            }
        }
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = (Socket)e.UserToken;
            if (s.Connected)
            {
                // close the socket associated with the client  
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                    // throws if client process has already closed  
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                    _Connected = false;
                }
            }
            Disconnect();
        }

        /// Disconnect from the host.  
        internal void Disconnect()
        {
            foreach (QSocketEventArgs arg in _ListArgs)
                arg.Completed -= IO_Completed;

            _ListArgs.Clear();
            _ReceiveEventArgs.Completed -= IO_Completed;
            _keepliveCode = "";
            _Connected = false;
            if (_ClientSocket.Connected)
            {
                _ClientSocket.Disconnect(true);
                Closed?.Invoke(this);
            }
            if (_Reconnect)
            {
                QCrontab.RunOnceWithTime(DateTime.Now.AddSeconds(10), () => { this.Connect(_ClientName); }, "重连");
            }
        }
        public void ReturnOK(string callBackCommand)
        {
            this.SendAsync(callBackCommand, new AckItem());
        }
        public void Return(string callBackCommand, object data)
        {
            this.SendAsync(callBackCommand, new AckItem(data));
        }
        public void ReturnError(string callBackCommand, string errMsg)
        {
            this.SendAsync(callBackCommand, new AckItem(-1, errMsg));
        }


        public void Dispose()
        {
            _AutoConnectEvent.Close();
            if (_ClientSocket.Connected)
            {
                _ClientSocket.Close();
            }
        }
    }
}
