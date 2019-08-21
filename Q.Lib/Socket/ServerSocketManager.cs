using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Q.Lib.Socket.ServerSocketAsync;

namespace Q.Lib.Socket
{
    public class ServerSocketManager
    {
        public static readonly ServerSocketManager Instance = new ServerSocketManager();
        ServerSocketAsync _serverSocket;
        List<SocketClientItem> _clientItems = new List<SocketClientItem>();
        ConcurrentDictionary<string, Action<ReceiveEventArgs, Exception, dynamic>> _actions = new ConcurrentDictionary<string, Action<ReceiveEventArgs, Exception, dynamic>>();
        public Action<SocketClientItem> RegisterClientEvent = null;
        public Action<SocketClientItem> RemoveClientEvent = null;

        public void Start(int port)
        {
            _serverSocket = new ServerSocketAsync(port);
            _serverSocket.Receive += (s, e) =>
            {
                Task.Run(() =>
                {

                    var actionKey = e.Messager.Action;
                    if (_actions.TryGetValue(actionKey, out Action<ReceiveEventArgs, Exception, dynamic> action))
                    {
                        action?.Invoke(e, e.Messager.Exception, e.Messager.Arg);
                    }
                    else
                    {
                        QLog.SendLog($"命令[{actionKey}] 不存在");
                        e.AcceptSocket.Write(new SocketMessager("UnKnow Command", "未知命令", new { ResCode = -1, ResDesc = "未知命令" }));
                    }
                });


            };
            _serverSocket.Accepted += (s, e) =>
            {
                QLog.SendLog($"新连接：{e.AcceptSocket.Id}({e.AcceptSocket.RemoteEndPoint.ToString()})");
                //延迟10s执行一次检测连接是否已注册
                QCrontab.RunWithDelay(10, () =>
                {
                    lock (_clientItems)
                    {
                        if (!_clientItems.Exists(x => x.ClientID == e.AcceptSocket.Id))
                        {
                            e.AcceptSocket.Write(new SocketMessager("S_Close", new { ResCode = -1002, ResDesc = "超过10s未进行注册，你已被踢下线" }));
                            QLog.SendLog($"Socket:{e.AcceptSocket.Id}({e.AcceptSocket.RemoteEndPoint.ToString()}) 超过10s未进行注册，现已踢除");
                            e.AcceptSocket.Close();
                        }
                    }
                }, "", "终端注册检查_" + e.AcceptSocket.Id);
            };
            _serverSocket.Closed += (s, e) =>
            {
                QLog.SendLog($"关闭了连接：{e.AcceptSocketId}");
                SocketClientItem client = null;
                lock (_clientItems)
                {
                    client = _clientItems.FirstOrDefault(x => x.ClientID == e.AcceptSocketId);
                    if (client != null)
                    {
                        _clientItems.Remove(client);
                    }
                }
                RemoveClientEvent?.Invoke(client);
            };
            _serverSocket.Error += (a, b) =>
            {
                QLog.SendLog_Exception($"发生错误({b.Errors})：{ b.Exception.Message + b.Exception.StackTrace}");
            };

            //注册->注册命令
            this.RegisterAction("S_Register", (r, e, d) =>
            {

                string clientName = d?.ClientName?.ToString();
                if (string.IsNullOrEmpty(clientName))
                {
                    QLog.SendLog($"{r.AcceptSocket.Id} 注册名称为空，注册失败，已断开连接");
                    r.AcceptSocket.AccessDenied();
                    QCrontab.CancleTask("终端注册检查_" + r.AcceptSocket.Id);
                }
                else
                {
                    var newClient = new SocketClientItem { AcceptSocket = r.AcceptSocket, ClientID = r.AcceptSocket.Id, ClientName = clientName };
                    lock (_clientItems)
                    {
                        var hisClients = _clientItems.FindAll(x => x.ClientName == clientName);
                        foreach (var client in hisClients)
                        {
                            client.SendCommand("S_CloseMsg", new { ResCode = -1002, ResDesc = "有同名称终端连接，你已被挤下线" });
                            _clientItems.Remove(client);
                            client.AcceptSocket.Close();
                        }
                        _clientItems.Add(newClient);
                    }
                    var msg = r.Messager.GetServerBackMessager(new { ResCode = 0 });
                    r.AcceptSocket.Write(msg);
                    QLog.SendLog($"{clientName} 注册成功");
                    QCrontab.CancleTask("终端注册检查_" + r.AcceptSocket.Id);
                    RegisterClientEvent?.Invoke(newClient);
                }

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

        public bool RegisterAction(string actionKey, Action<ReceiveEventArgs, Exception, dynamic> action)
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
        /// <param name="args"></param>
        public void SendCommand(string clientName, string actionKey, object args, Action<ReceiveEventArgs, Exception, dynamic> callBack = null, int timeOut = 30)
        {
            lock (_clientItems)
            {
                var client = _clientItems.FirstOrDefault(x => x.ClientName == clientName);
                if (client != null)
                {
                    Task.Run(() =>
                    {
                        args = args ?? new object();
                        if (callBack == null)
                        {
                            client.AcceptSocket.Write(new SocketMessager(actionKey, args));
                        }
                        else
                        {
                            client.AcceptSocket.Write(new SocketMessager(actionKey, args), (s, e) =>
                            {

                                callBack?.Invoke(e, e.Messager.Exception, e.Messager.Arg);
                            }, TimeSpan.FromSeconds(30));
                        }
                    });
                }
            }
        }


    }
}
