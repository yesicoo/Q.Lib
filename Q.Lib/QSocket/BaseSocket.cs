using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.QSocket
{
    public class BaseSocket
    {
        internal byte[] WriteStream(string str)
        {

            var strBytes = Encoding.UTF8.GetBytes(str);
            byte[] buff = Encoding.UTF8.GetBytes(Convert.ToString(strBytes.Length + 8, 16).PadRight(8));
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(buff, 0, buff.Length);
                ms.Write(strBytes, 0, strBytes.Length);
                return ms.ToArray();
            }
        }
        internal string ReadStream(Stream stream)
        {
            byte[] data = new byte[8];

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
            overs -= 8;
            using (MemoryStream ms = new MemoryStream())
            {
                data = new byte[1024];
                while (overs > 0)
                {
                    bytes = stream.Read(data, 0, overs < data.Length ? overs : data.Length);
                    overs -= bytes;
                    ms.Write(data, 0, bytes);
                }
                var length = ms.Length;
                if (length > int.MaxValue)
                {
                    string str = "";
                    var by = ms.ToArray();
                    for (long i = 0; i < length; i += int.MaxValue)
                    {
                        int readlength = (length - i > Convert.ToInt64(int.MaxValue) ? int.MaxValue : Convert.ToInt32(length - i));
                        str += Encoding.UTF8.GetString(by, Convert.ToInt32(i), readlength);
                    }
                    return str;
                }
                else
                {
                    return Encoding.UTF8.GetString(ms.ToArray(), 0, Convert.ToInt32(length));
                }

            }
        }
    }
}
