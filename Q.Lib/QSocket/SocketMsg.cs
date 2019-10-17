using Q.Lib.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    public class SocketMsg
    {
        public string Command { set; get; }
        public dynamic Data { set; get; }
        public string CallBackCommand { set; get; }

        public T GetObj<T>()
        {
            return Json.Convert2T<T>(Data);
        }
    }
}
