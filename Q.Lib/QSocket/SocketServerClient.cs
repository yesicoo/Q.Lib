using Q.Lib.Extension;
using Q.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    public class SocketServerClient
    {
        public int Index { set; get; }
        public string ClientName { get; internal set; }
        public System.Net.Sockets.Socket Socket { get; internal set; }
        public SocketAsyncEventArgs SocketAsyncEventArgs { get; internal set; }

        public void CallBack(string callBackCommand, AckItem ack)
        {
            var data = Encoding.UTF8.GetBytes(Json.ToJsonStr(new { Command = callBackCommand, Data = ack }));
            Array.Copy(data, 0, this.SocketAsyncEventArgs.Buffer, 0, data.Length);//设置发送数据
            this.Socket.SendAsync(this.SocketAsyncEventArgs);
        }
    }
}
