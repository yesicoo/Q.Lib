using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Q.Lib.Socket.ServerSocketAsync;

namespace Q.Lib.Socket
{
    public class SocketClientItem
    {
        public int ClientID { set; get; }
        public string ClientName { get; internal set; }
        public AcceptSocket AcceptSocket { get; internal set; }

        /// <summary>
        /// 发送消息给客户端
        /// </summary>
        /// <param name="clientName"></param>
        /// <param name="actionKey"></param>
        /// <param name="args"></param>
        public void SendCommand(string actionKey, object args, Action<ReceiveEventArgs, Exception, dynamic> callBack = null, int timeOut = 30)
        {
            lock (this)
            {
                Task.Run(() =>
                {
                    args = args ?? new object();
                    if (callBack == null)
                    {
                        this.AcceptSocket.Write(new SocketMessager(actionKey, args));
                    }
                    else
                    {
                        this.AcceptSocket.Write(new SocketMessager(actionKey, args), (s, e) =>
                        {
                            callBack?.Invoke(e, e.Messager.Exception, e.Messager.Arg);
                        }, TimeSpan.FromSeconds(30));
                    }
                });

            }
        }
    }
}
