using Q.Lib.Extension;
using Q.Lib.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Q.Lib.Socket.ServerSocketAsync;

namespace Q.Lib.Socket
{
    public class ServerSocketManager
    {
        public static readonly ServerSocketManager Instance = new ServerSocketManager();
        ServerSocketAsync _serverSocket;
        List<SocketClientItem> _clientItems = new List<SocketClientItem>();
        ConcurrentDictionary<string, Action<ReceiveEventArgs, AckItem>> _actions = new ConcurrentDictionary<string, Action<ReceiveEventArgs, AckItem>>();
        public Action<SocketClientItem> RegisterClientEvent = null;
        public Action<SocketClientItem> RemoveClientEvent = null;
        public Action<SocketClientItem, Exception> ClientErrorEvent = null;

        public void Start(int port)
        {
            _serverSocket = new ServerSocketAsync(port);
            _serverSocket.Receive += (s, e) =>
            {
                Task.Run(() =>
                {

                    var actionKey = e.Messager.Action;
                    if (_actions.TryGetValue(actionKey, out Action<ReceiveEventArgs, AckItem> action))
                    {
                        action?.Invoke(e, e.Messager.Data);
                    }
                    else
                    {
                        QLog.SendLog_Debug($"命令[{actionKey}] 不存在");
                        e.ReturnError("未知命令", -101);
                    }
                });


            };
            _serverSocket.Accepted += (s, e) =>
            {
                QLog.SendLog_Debug($"新连接：{e.AcceptSocket.Id}({e.AcceptSocket.RemoteEndPoint.ToString()})");
                //延迟10s执行一次检测连接是否已注册
                QCrontab.RunWithDelay(10, () =>
                {
                    lock (_clientItems)
                    {
                        if (!_clientItems.Exists(x => x.ClientID == e.AcceptSocket.Id))
                        {
                            e.AcceptSocket.Write(new SocketMessager("S_Close", new AckItem(-1002, "超过10s未进行注册，你已被踢下线")));
                            QLog.SendLog_Debug($"Socket:{e.AcceptSocket.Id}({e.AcceptSocket.RemoteEndPoint.ToString()}) 超过10s未进行注册，现已踢除");
                            e.AcceptSocket.Close();
                        }
                    }
                }, "", "终端注册检查_" + e.AcceptSocket.Id);
            };
            _serverSocket.Closed += (s, e) =>
            {

                SocketClientItem client = null;
                lock (_clientItems)
                {
                    client = _clientItems.FirstOrDefault(x => x.ClientID == e.AcceptSocketId);
                    if (client != null)
                    {
                        _clientItems.Remove(client);
                        QLog.SendLog_Debug($"关闭了连接：{client.ClientName}({e.AcceptSocketId})");
                    }
                }
                Task.Run(() =>
                {
                    RemoveClientEvent?.Invoke(client);
                });
            };
            _serverSocket.Error += (a, b) =>
            {
                lock (_clientItems)
                {
                    var client = _clientItems.FirstOrDefault(x => x.ClientID == b.AcceptSocket.Id);
                    if (client != null)
                    {
                        Task.Run(() => { ClientErrorEvent?.Invoke(client, b.Exception); });
                    }
                }

                QLog.SendLog_Debug($"发生错误({b.Errors})：{ b.Exception.Message + b.Exception.StackTrace}");
            };

            //注册->注册命令
            this.RegisterAction(SocketMessager.SYS_REGIST.Action, (e, d) =>
            {

                string clientName = d.ResData?.ClientName?.ToString();
                if (string.IsNullOrEmpty(clientName))
                {
                    QLog.SendLog_Debug($"{e.AcceptSocket.Id} 注册名称为空，注册失败，已断开连接");
                    e.AcceptSocket.AccessDenied();
                    QCrontab.CancleTask("终端注册检查_" + e.AcceptSocket.Id);
                }
                else
                {
                    var newClient = new SocketClientItem { AcceptSocket = e.AcceptSocket, ClientID = e.AcceptSocket.Id, ClientName = clientName };
                    lock (_clientItems)
                    {
                        var hisClients = _clientItems.FindAll(x => x.ClientName == clientName);
                        foreach (var client in hisClients)
                        {
                            client.SendCommand("S_Close", new { Status = "Error", Msg = "有同名称终端连接，你已被挤下线" });
                            _clientItems.Remove(client);
                            client.AcceptSocket.Close();
                        }
                        _clientItems.Add(newClient);
                    }
                    var msg = e.Messager.GetServerBackMessager(new AckItem());
                    e.AcceptSocket.Write(msg);
                    QLog.SendLog($"{clientName} 注册成功");
                    QCrontab.CancleTask("终端注册检查_" + e.AcceptSocket.Id);
                    Task.Run(() =>
                    {
                        RegisterClientEvent?.Invoke(newClient);
                    });
                }

            });
            this.RegisterAction(SocketMessager.SYS_TEST_ECHO.Action, (e, d) =>
            {
                var msg = e.Messager.GetServerBackMessager(d);
                e.AcceptSocket.Write(msg);
            });
            _serverSocket.Start();
            QLog.SendLog($"Socket Server Start With {port} Port");
        }

        internal void UpdateClientName(string clientName, string newAngentName)
        {
            lock (_clientItems)
            {
                var client = _clientItems.FirstOrDefault(x => x.ClientName == clientName);
                if (client != null)
                {
                    client.ClientName = newAngentName;
                }
            }
        }

        public void Stop()
        {
            lock (_clientItems)
            {
                _clientItems.Clear();
            }
            _actions?.Clear();
            _serverSocket?.Stop();
        }

        public List<SocketClientItem> AllClients()
        {
            return _clientItems;
        }
        public SocketClientItem GetClient(string clientName)
        {
            return _clientItems.FirstOrDefault(x => x.ClientName == clientName);
        }

        internal bool SocketServerStatus()
        {
            return _serverSocket.IsRuning();
        }

        public bool RegisterAction(string actionKey, Action<ReceiveEventArgs, AckItem> action)
        {
            if (string.IsNullOrEmpty(actionKey) || action == null)
            {
                QLog.SendLog($"命令[{actionKey}] 或这执行方法无效");
                return false;
            }

            if (_actions.ContainsKey(actionKey))
            {
                QLog.SendLog($"命令[{actionKey}] 已存在，请勿重复添加");
                return false;
            }
            else
            {
                return _actions.TryAdd(actionKey, action);

            }
        }

        /// <summary>
        /// 发送消息给客户端
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="actionKey"></param>
        /// <param name="arg"></param>
        public void SendCommand(string clientName, string actionKey, object arg, Action<ReceiveEventArgs, AckItem> callBack = null, int timeOut = 30)
        {
            lock (_clientItems)
            {
                var client = _clientItems.FirstOrDefault(x => x.ClientName == clientName);
                if (client != null)
                {
                    Task.Run(() =>
                    {
                        var data = new AckItem(arg);
                        if (callBack == null)
                        {
                            client.AcceptSocket.Write(new SocketMessager(actionKey, data));
                        }
                        else
                        {
                            client.AcceptSocket.Write(new SocketMessager(actionKey, data), (s, e) =>
                            {
                                callBack?.Invoke(e, e.Messager.Data);

                                Task.Run(() => { BaseSocket.SocketLog?.Invoke(actionKey, arg, e.Messager.Data); });
                            }, TimeSpan.FromSeconds(timeOut));
                        }
                    });
                }
            }
        }

        public T SendData<T>(string clientName, string actionKey, object arg, int timeOut = 30)
        {
            T t = default(T);
            SocketClientItem client = null;
            lock (_clientItems)
            {
                client = _clientItems.FirstOrDefault(x => x.ClientName == clientName);
            }

            if (client != null)
            {
                lock (client)
                {
                    ManualResetEvent resetEvent = new ManualResetEvent(false);

                    resetEvent.Set();
                    var data = new AckItem(arg);
                    client.AcceptSocket.Write(new SocketMessager(actionKey, data), (s, e) =>
                    {
                        t = Json.Convert2T<T>(e.Messager.Data?.ResData);
                        
                        Task.Run(() => { BaseSocket.SocketLog?.Invoke(actionKey, arg, e.Messager.Data); });
                        resetEvent.Set();
                    });
                    resetEvent.WaitOne(TimeSpan.FromSeconds(timeOut));
                }
            }
            return t;
        }

        public AckItem SendData(string clientName, string actionKey, object arg, int timeOut = 30)
        {
            SocketClientItem client = null;
            AckItem result = null;
            lock (_clientItems)
            {
                client = _clientItems.FirstOrDefault(x => x.ClientName == clientName);
            }

            if (client != null)
            {
                lock (client)
                {
                    ManualResetEvent resetEvent = new ManualResetEvent(false);
                    var data = new AckItem(arg);
                  
                    client.AcceptSocket.Write(new SocketMessager(actionKey, data), (s, e) =>
                    {
                        result = e.Messager.Data;
                        Task.Run(() => { BaseSocket.SocketLog?.Invoke(actionKey, arg, e.Messager.Data); });
                        resetEvent.Set();
                    });
                    resetEvent.WaitOne(TimeSpan.FromSeconds(timeOut));
                }
            }
            return result;
        }

    }
}
