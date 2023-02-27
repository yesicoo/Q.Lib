using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Q.Lib
{
    public static class QTools
    {
        #region 随机生成字符串
        /// <summary>
        /// 随机生成字符串
        /// </summary>
        /// <param name="num">位数</param>
        /// <param name="type">
        /// 0 -区分大小写字符包含数字的随机字符串
        /// 1 -小写字符含数字字符串
        /// 2 -大写字符包含数字字符串
        /// 3 -小写字符随机字符串
        /// 4 -大写字符随机字符串
        /// 5 -仅数字
        /// </param>
        /// <returns></returns>
        public static string RandomCode(int num, int type = 0)
        {
            string chars = null;

            switch (type)
            {
                case 0:
                    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghicklnopqrstuvwxyz1234567890";
                    break;
                case 1:
                case 2:
                    chars = "abcdefghicklnopqrstuvwxyz1234567890";
                    break;
                case 3:
                case 4:
                    chars = "abcdefghicklnopqrstuvwxyz";
                    break;
                case 5:
                    chars = "1234567890";
                    break;
                default:
                    break;
            }
            if (!string.IsNullOrEmpty(chars))
            {
                int length = chars.Length;
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < num; i++)
                {
                    Random r = new Random(BitConverter.ToInt32(System.Guid.NewGuid().ToByteArray(), 0));
                    sb.Append(chars[r.Next(0, length)]);
                }
                var result = sb.ToString();
                if (type == 2 || type == 4)
                {
                    result = result.ToUpper();
                }
                return result;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 随机生成字符串（自定义因子）
        /// </summary>
        /// <param name="num"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static string RandomCode(int num, string chars)
        {
           
            if (!string.IsNullOrEmpty(chars))
            {
                int length = chars.Length;
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < num; i++)
                {
                    Random r = new Random(BitConverter.ToInt32(System.Guid.NewGuid().ToByteArray(), 0));
                    sb.Append(chars[r.Next(0, length)]);
                }
                return sb.ToString();
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 随机元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="num"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static T[] RandomItems<T>(int num, IEnumerable<T> ts)
        {
            var items = ts.ToArray();
            if (items.Length>0)
            {
                int length = items.Length;
                T[] result = new T[num];

                for (int i = 0; i < num; i++)
                {
                    Random r = new Random(BitConverter.ToInt32(System.Guid.NewGuid().ToByteArray(), 0));
                    result[i]=(items[r.Next(0, length)]);
                }
                return result;
            }
            else
            {
                return default(T[]);
            }
        }
        public static T RandomItem<T>(IEnumerable<T> ts)
        {
            var items = ts.ToArray();
            if (items.Length > 0)
            {
                int length = items.Length;

                Random r = new Random(BitConverter.ToInt32(System.Guid.NewGuid().ToByteArray(), 0));
                return (items[r.Next(0, length)]);
            }
            else
            {
                return default(T);
            }
        }
        #endregion



        #region 时间戳转换
        /// <summary>  
        /// 获取时间戳  
        /// </summary>  
        /// <returns></returns>  
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        /// <summary>
        /// unix时间戳转换成日期
        /// </summary>
        /// <param name="unixTimeStamp">时间戳（秒）</param>
        /// <returns></returns>
        public static DateTime UnixTimestampToDateTime(this DateTime target, long timestamp)
        {
            var start = new DateTime(1970, 1, 1, 0, 0, 0);
            return start.AddSeconds(timestamp).ToLocalTime();
        }
        #endregion

        #region GUID字符串
        /// <summary>
        /// GUID字符串 32位
        /// </summary>
        /// <returns></returns>
        public static string GuidStr()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }
        /// <summary>
        /// 16位字符串
        /// </summary>
        /// <returns></returns>
        public static string GuidShortStr()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
            {
                i *= ((int)b + 1);
            }
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }
        /// <summary>
        /// 19位数字
        /// </summary>
        /// <returns></returns>
        public static long GuidNum()
        {
            byte[] buffer = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }
        #endregion


        #region 获取字符串MD5加密
        /// <summary>
        /// 获取字符串MD5加密
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetMD5Hash(string input)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
            bs = md5.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            string password = s.ToString();
            return password;
        }
        #endregion

        #region GetMD5
        /// <summary>
        /// GetMD5
        /// </summary>
        /// <param name="encypStr"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string GetMD5(string encypStr, string charset)
        {
            string retStr;
            MD5CryptoServiceProvider m5 = new MD5CryptoServiceProvider();

            //创建md5对象
            byte[] inputBye;
            byte[] outputBye;

            //使用GB2312编码方式把字符串转化为字节数组．
            try
            {

                inputBye = Encoding.GetEncoding(charset).GetBytes(encypStr);
            }
            catch (Exception)
            {
                inputBye = Encoding.GetEncoding("GB2312").GetBytes(encypStr);
            }
            outputBye = m5.ComputeHash(inputBye);

            retStr = System.BitConverter.ToString(outputBye);
            retStr = retStr.Replace("-", "").ToUpper();
            return retStr;
        }
        #endregion

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="unifiedorder"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string SignValue(SortedDictionary<string, string> unifiedorder, string key)
        {
            int i = 0;
            string sign = string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> temp in unifiedorder)
            {
                if (temp.Value == "" || temp.Value == null || temp.Key.ToLower() == "sign")
                {
                    continue;
                }
                i++;
                sb.Append(temp.Key.Trim() + "=" + temp.Value.Trim() + "&");
            }
            sb.Append("key=" + key.Trim() + "");
            string signkey = sb.ToString();
            QLog.SendLog(signkey);
            sign = GetMD5(signkey, "utf-8");

            return sign;
        }

        /// <summary>
        /// 把XML数据转换为SortedDictionary<string, string>集合
        /// </summary>
        /// <param name="strxml"></param>
        /// <returns></returns>
        public static SortedDictionary<string, string> GetInfoFromXml(string xmlstring)
        {
            SortedDictionary<string, string> sParams = new SortedDictionary<string, string>();
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlstring);
                XmlElement root = doc.DocumentElement;
                int len = root.ChildNodes.Count;
                for (int i = 0; i < len; i++)
                {
                    string name = root.ChildNodes[i].Name;
                    if (!sParams.ContainsKey(name))
                    {
                        sParams.Add(name.Trim(), root.ChildNodes[i].InnerText.Trim());
                    }
                }
            }
            catch { }
            return sParams;
        }
        /// <summary>
        /// 判断字符串空或者null
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(params string[] args)
        {
            foreach (var item in args)
            {
                if (string.IsNullOrEmpty(item))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 3DES 加密
        /// </summary>
        /// <returns></returns>
        public static string E3DES(string value, string key = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                return EncryptHelper.Encrypt3DES(value);
            }
            else
            {
                return EncryptHelper.Encrypt3DES(value, key);
            }
        }

        /// <summary>
        /// 3DES 解密
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string D3DES(string value, string key = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                return EncryptHelper.Decrypt3DES(value);
            }
            else
            {
                return EncryptHelper.Decrypt3DES(value, key);
            }
        }

        /// <summary>
        /// 程序运行超时检测方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ms"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        internal static T TimeoutCheck<T>(int ms, Func<T> func)
        {
            var wait = new ManualResetEvent(false);
            bool RunOK = false;
            var taskResult = Task.Run<T>(() =>
            {
                var result = func.Invoke();
                RunOK = true;
                wait.Set();
                return result;
            });
            wait.WaitOne(ms);
            if (RunOK)
            {
                return taskResult.Result;
            }
            else
            {
                return default(T);
            }
        }
        public static bool IsNOE(string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static string IfNOE(string value, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            else
            {
                return value;
            }
        }

        public static object RemoveProperty(object obj, params string[] PropertyNames)
        {
            JObject jObj = JObject.FromObject(obj);
            if (PropertyNames.Length > 0)
            {
                foreach (var name in PropertyNames)
                {
                    jObj.Remove(name);
                }
            }
            return jObj;

        }

        public static T RemoveProperty<T>(object obj,params string[] PropertyNames)
        {
            JObject jObj = JObject.FromObject(obj);
            if (PropertyNames.Length > 0)
            {
                foreach (var name in PropertyNames)
                {
                    jObj.Remove(name);
                }
            }
            return jObj.ToObject<T>();
        }
        /// <summary>
        /// 获取程序运行目录
        /// </summary>
        /// <returns></returns>
        public static string GetRuningDirPath()
        {
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

    }
}
