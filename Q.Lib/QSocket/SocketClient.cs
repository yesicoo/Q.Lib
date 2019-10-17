using Q.Lib.Extension;
using Q.Lib.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    public class SocketClient: BaseSocket
    {
        private TcpClient _Client;
        private int port;
        private IPAddress remote;

        public Action<SocketClient> Connected;
        public Action<SocketClient> Closed;
        public Action<Exception> Error;

        public bool IsRuning = false;

        private ConcurrentDictionary<string, Action<SocketClient, SocketMsg>> _Commands = new ConcurrentDictionary<string, Action<SocketClient, SocketMsg>>();

        private ConcurrentDictionary<string, Action<AckItem>> _CallBacks = new ConcurrentDictionary<string, Action<AckItem>>();

        string CrontabTaskID = null;
        public SocketClient(string address, int port)
        {
            this.remote = IPAddress.Parse(address);
            this.port = port;
        }
        public SocketClient(IPAddress remote, int port)
        {

            this.port = port;
            this.remote = remote;
        }



        public void Connect()
        {
            try
            {
                if (_Client != null)
                    Close();
                _Client = new TcpClient();

                _Client.Connect(remote, port);
                Connected?.Invoke(this);
                IsRuning = true;
                Watch();


            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                IsRuning = false;
            }
        }

        public void SendAsync(string command, object param, Action<AckItem> callBack)
        {
            try
            {
                if (IsRuning)
                {
                    string callBackCommand = null;
                    if (callBack != null)
                    {
                        callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";
                    }

                    var sendData = WriteStream(Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand }));
                    _Client.GetStream().WriteAsync(sendData, 0, sendData.Length);
                    _CallBacks.TryAdd(callBackCommand, callBack);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                Close();
            }
        }

        public AckItem SendSync(string command, object param, int timeOut = 30)
        {
            try
            {
                AckItem ack = new AckItem(-1, "客户端未连接");
                if (IsRuning)
                {
                    ack = new AckItem(-1, "请求超时");
                    ManualResetEvent resetEvent = new ManualResetEvent(false);
                    string callBackCommand = $"CallBack_{command}_{QTools.RandomCode(5)}";
                    var sendData = WriteStream(Json.ToJsonStr(new { Command = command, Data = param, CallBackCommand = callBackCommand }));
                    _Client.GetStream().WriteAsync(sendData, 0, sendData.Length);
                    _CallBacks.TryAdd(callBackCommand, (r) =>
                    {

                        ack = r;
                        resetEvent.Set();
                    });
                    resetEvent.WaitOne(TimeSpan.FromSeconds(timeOut));
                    return ack;
                }
                return ack;
            }
            catch (Exception ex)
            {
                Error?.Invoke(ex);
                Close();
                return new AckItem(-1, ex.Message);
            }
        }

        private void Watch()
        {
            Task.Run(() =>
            {
                while (IsRuning)
                {
                    NetworkStream ns = this._Client.GetStream();
                    ns.ReadTimeout = 1000 * 30;
                    if (ns.DataAvailable)
                    {
                        string dataStr = ReadStream(ns);
                        if (dataStr.StartsWith("Ping"))
                        {
                            var returnData = WriteStream("Pong");
                            ns.Write(returnData, 0, returnData.Length);
                        }
                        else if (dataStr.StartsWith("{"))
                        {
                            SocketMsg socketMsg = Json.ToObj<SocketMsg>(dataStr);

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
                                QLog.SendLog_Debug("未找到命令:" + dataStr);
                            }
                        }
                        else
                        {
                            QLog.SendLog("未知消息:" + dataStr);
                        }
                    }
                }
            });
        }


        public void Close()
        {
            try
            {
                _Client.Close();
#if NET45
    //不支持
#else
                _Client.Dispose();
#endif
            }
            finally
            {
                _Client = null;
                IsRuning = false;
                QCrontab.CancleTask(CrontabTaskID);
                CrontabTaskID = null;
                Closed?.Invoke(this);
            }
        }

        private void KeepLive()
        {
            CrontabTaskID = QCrontab.RunWithSecond(20, () =>
             {
                 if (IsRuning)
                 {
                     if (_Client.Connected)
                     {
                         try
                         {
                             var pingData = WriteStream("Ping");
                             _Client.GetStream().Write(pingData, 0, pingData.Length);
                         }
                         catch (Exception ex)
                         {
                             Error.Invoke(ex);
                             Close();
                         }
                     }
                     else
                     {
                         Close();
                     }
                 }
                 else
                 {
                     QCrontab.CancleTask(CrontabTaskID);
                 }
             }, "KeepLive");
        }

        
    }
}
