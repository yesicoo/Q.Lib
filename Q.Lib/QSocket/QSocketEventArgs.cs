using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    class QSocketEventArgs : SocketAsyncEventArgs
    {

        /// <summary>  
        /// 标识，只是一个编号而已  
        /// </summary>  
        public int ArgsTag { get; set; }

        /// <summary>  
        /// 设置/获取使用状态  
        /// </summary>  
        public bool IsUsing { get; set; }

    }
}
