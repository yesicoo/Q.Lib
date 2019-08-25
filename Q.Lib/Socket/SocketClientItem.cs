using Q.Lib.Extension;
using Q.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public void SendCommand(string actionKey, object args, Action<ReceiveEventArgs, AckItem> callBack = null, int timeOut = 30)
        {
            lock (this)
            {
                Task.Run(() =>
                {
                    var data = new AckItem(args);

                    this.AcceptSocket.Write(new SocketMessager(actionKey, data), (s, e) =>
                    {
                        callBack?.Invoke(e, e.Messager.Data);

                        Task.Run(() => { BaseSocket.SocketLog?.Invoke(actionKey, args, e.Messager.Data); });
                    }, TimeSpan.FromSeconds(timeOut));

                });

            }
        }

        public T SendData<T>(string actionKey, object arg, int timeOut = 30)
        {
            T t = default(T);
            lock (this)
            {
                ManualResetEvent resetEvent = new ManualResetEvent(true);
                resetEvent.Set();
                var data = new AckItem(arg);
                this.AcceptSocket.Write(new SocketMessager(actionKey, data), (s, e) =>
                {
                    t = Json.ToObj<T>(Newtonsoft.Json.JsonConvert.SerializeObject(e.Messager.Data?.ResData));
                    resetEvent.Reset();
                    Task.Run(() => { BaseSocket.SocketLog?.Invoke(actionKey, arg, e.Messager.Data); });
                });
                resetEvent.WaitOne(TimeSpan.FromSeconds(timeOut));

            }
            return t;
        }
    }
}
