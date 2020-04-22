using Q.Lib.Extension;
using Q.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    public class ServerClient : BaseSocket
    {
        /// <summary>  
        /// 客户端IP地址  
        /// </summary>  
        public IPAddress IPAddress { get; set; }

        /// <summary>  
        /// 远程地址  
        /// </summary>  
        public EndPoint Remote { get; set; }

        /// <summary>  
        /// 通信SOKET  
        /// </summary>  
        public System.Net.Sockets.Socket Socket { get; set; }

        /// <summary>  
        /// 连接时间  
        /// </summary>  
        public DateTime ConnectTime { get; set; }

        ///// <summary>  
        ///// 所属用户信息  
        ///// </summary>  
        //public UserInfoModel UserInfo { get; set; }

        public string ClientName { set; get; }

        public int Index { set; get; }

        public Action<string, string> Log;

        /// <summary>  
        /// 数据缓存区  
        /// </summary>  
        public List<byte> Buffer { get; set; }

        public string CrontabTaskID { get; internal set; }

        public ServerClient()
        {
            this.Buffer = new List<byte>();
        }



        public void CallBack(string callBackCommand, AckItem ack)
        {
            if (!string.IsNullOrEmpty(callBackCommand))
            {
                var datastr = Json.ToJsonStr(new { Command = callBackCommand, Data = ack });
                var data = WriteStream(datastr);

                SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                sendArg.UserToken = this;
                sendArg.SetBuffer(data, 0, data.Length);  //将数据放置进去.  
                this.Socket.SendAsync(sendArg);
                if (Log != null)
                {
                    Task.Run(() => { Log.Invoke("Send", $"[{ClientName}]{datastr}"); });
                }
            }
        }

        public void Return(string command, object obj)
        {
            if (!string.IsNullOrEmpty(command))
            {
                CallBack(command, new AckItem(obj));
            }
        }
        public void ReturnOK(string command)
        {
            if (!string.IsNullOrEmpty(command))
            {
                CallBack(command, new AckItem());
            }
        }

        public void ReturnError(string command, string resDesc)
        {
            if (!string.IsNullOrEmpty(command))
            {
                CallBack(command, new AckItem(-1, resDesc));
            }
        }
    }
}
