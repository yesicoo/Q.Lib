using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib.Socket
{
    public class BaseSocket
    {
        public static   Action<string,object,object> SocketLog=null;
        public static int HeadLength = 8;
        public static byte[] Read(Stream stream, byte[] end)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] data = new byte[1];
                int bytes = data.Length;
                while (bytes > 0 && BaseSocket.findBytes(ms.ToArray(), end, 0) == -1)
                {
                    bytes = stream.Read(data, 0, data.Length);
                    ms.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }
        protected void Write(Stream stream, SocketMessager messager)
        {
            var buffer = this.GetWriteBuffer(messager);
            stream.Write(buffer, 0, buffer.Length);
        }
        protected void WriteAsync(Stream stream, SocketMessager messager)
        {
            var buffer = this.GetWriteBuffer(messager);
            stream.WriteAsync(buffer, 0, buffer.Length);
        }
        protected byte[] GetWriteBuffer(SocketMessager messager)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buff = Encoding.UTF8.GetBytes(messager.GetCanParseString());
                ms.Write(buff, 0, buff.Length);
                if (messager.Data != null)
                {
                    var data = BaseSocket.Serialize(messager.Data);
                    using (MemoryStream msBuf = new MemoryStream())
                    {
                        using (DeflateStream ds = new DeflateStream(msBuf, CompressionMode.Compress))
                        {
                            ds.Write(data, 0, data.Length);
                            buff = msBuf.ToArray();
                            ms.Write(buff, 0, buff.Length);
                        }
                    }
                }
                return this.GetWriteBuffer(ms.ToArray());
            }
        }
        private byte[] GetWriteBuffer(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buff = Encoding.UTF8.GetBytes(Convert.ToString(data.Length + BaseSocket.HeadLength, 16).PadRight(BaseSocket.HeadLength));
                ms.Write(buff, 0, buff.Length);
                ms.Write(data, 0, data.Length);
                return ms.ToArray();
            }
        }

        protected SocketMessager Read(Stream stream)
        {
            byte[] data = new byte[BaseSocket.HeadLength];
            int bytes = 0;
            int overs = data.Length;
            string size = string.Empty;
            while (overs > 0)
            {
                bytes = stream.Read(data, 0, overs);
                overs -= bytes;
                size += Encoding.UTF8.GetString(data, 0, bytes);
            }

            if (int.TryParse(size, NumberStyles.HexNumber, null, out overs) == false)
            {
                return null;
            }
            overs -= BaseSocket.HeadLength;
            using (MemoryStream ms = new MemoryStream())
            {
                data = new Byte[1024];
                while (overs > 0)
                {
                    bytes = stream.Read(data, 0, overs < data.Length ? overs : data.Length);
                    overs -= bytes;
                    ms.Write(data, 0, bytes);
                }
                return SocketMessager.Parse(ms.ToArray());
            }
        }

        public static byte[] Serialize(object obj)
        {
            return Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }
        public static object Deserialize(byte[] stream)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(Encoding.UTF8.GetString(stream));
        }

        public static int findBytes(byte[] source, byte[] find, int startIndex)
        {
            if (find == null) return -1;
            if (find.Length == 0) return -1;
            if (source == null) return -1;
            if (source.Length == 0) return -1;
            if (startIndex < 0) startIndex = 0;
            int idx = -1, idx2 = startIndex - 1;
            do
            {
                idx2 = idx = Array.FindIndex<byte>(source, Math.Min(idx2 + 1, source.Length), delegate (byte b)
                {
                    return b == find[0];
                });
                if (idx2 != -1)
                {
                    for (int a = 1; a < find.Length; a++)
                    {
                        if (++idx2 >= source.Length || source[idx2] != find[a])
                        {
                            idx = -1;
                            break;
                        }
                    }
                    if (idx != -1) break;
                }
            } while (idx2 != -1);
            return idx;
        }

       
    }

    public class SocketMessager
    {
        private static int _identity;
        internal static readonly SocketMessager SYS_TEST_LINK = new SocketMessager("\0");
        internal static readonly SocketMessager SYS_HELLO_WELCOME = new SocketMessager("Hello, Welcome!");
        internal static readonly SocketMessager SYS_ACCESS_DENIED = new SocketMessager("Access Denied.");
        internal static readonly SocketMessager SYS_QUIT = new SocketMessager("Hi, Bye Bye!");
        internal static readonly SocketMessager SYS_REGIST = new SocketMessager("S_Regist");
        internal static readonly SocketMessager SYS_CLOSE = new SocketMessager("S_Close");
        internal static readonly SocketMessager SYS_TEST_ECHO = new SocketMessager("S_Echo");

        private int _id;
        private string _action;
        private DateTime _remoteTime;
        private dynamic _data;
        private Exception _exception;

        public SocketMessager(string action)
            : this(action, null)
        {
        }
        public SocketMessager(string action, object data)
        {
            this._id = Interlocked.Increment(ref _identity);
            this._action = action == null ? string.Empty : action;
            this._data = data;
            this._remoteTime = DateTime.Now;
        }
        public override string ToString()
        {
            return
                this._remoteTime.ToString("yyyy-MM-dd HH:mm:ss") + "\t" +
                this._id + "\t" +
                this._action.Replace("\t", "\\t") + "\t" +
                this._data;
        }

        public string GetCanParseString()
        {
           

            if (string.Compare(this._action, SocketMessager.SYS_TEST_LINK.Action) == 0)
            {
                return this.Action;
            }

            var p = new MsgItem
            {
                ID = this._id,
                Action = this._action,
                RemoteTime = this._remoteTime,
                Data = this._data
            };

            return Q.Lib.Extension.Json.ToJsonStr(p);
        }

        public static SocketMessager Parse(byte[] data)
        {
            if (data == null) return new SocketMessager("NULL");
            if (data.Length == 1 && data[0] == 0) return SocketMessager.SYS_TEST_LINK;
            string text = Encoding.UTF8.GetString(data);
            var msgItem = Q.Lib.Extension.Json.ToObj<MsgItem>(text);

            SocketMessager messager = new SocketMessager(msgItem.Action,msgItem.Data);
            QLog.SendLog_Debug("----->"+messager.Action);
            messager._id = msgItem.ID;
            messager._remoteTime = msgItem.RemoteTime;
            if (messager._data is Exception) messager._exception = messager._data as Exception;
            return messager;
        }

        public SocketMessager GetServerBackMessager(object args)
        {
            var newMsg = new SocketMessager(this.Action+"_CallBack", args);
            newMsg.Id = this.Id;
            return newMsg;
        }

        /// <summary>
        /// 消息ID，每个一消息ID都是惟一的，同步发送时用
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }
        public string Action
        {
            get { return _action; }
        }
        public DateTime RemoteTime
        {
            get { return _remoteTime; }
        }
        public dynamic Data
        {
            get { return _data; }
        }
        public Exception Exception
        {
            get { return _exception; }
        }
    }

    internal class MsgItem
    {
        public int ID { set; get; }
        public string Action { set; get; }
        public DateTime RemoteTime { set; get; }

        public dynamic Data { set; get; }

    }
}
