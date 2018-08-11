using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib
{
    public static class WebClientHelper
    {
        public static string Post(string url, string parameter)
        {
            string result = string.Empty;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers["Accept-Language"] = "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2";
                    wc.Headers["Content-Type"] = "application/json";
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/537.36";

                    if (parameter == null)
                    {
                        result = Encoding.UTF8.GetString(wc.UploadData(url, "POST", new byte[] { }));
                    }
                    else
                    {
                        result = Encoding.UTF8.GetString(wc.UploadData(url, "POST", Encoding.UTF8.GetBytes(parameter)));
                    }

                }
                catch (Exception ex)
                {
                    result = null;
                    ex.Message.SendLog_Exception();
                    $"【error】{url}|{parameter}".SendLog_Debug();
                }
            }
            return result;
        }

        public static T Post<T>(string url, string parameter)
        {
            string result = string.Empty;
            T t_result = default(T);
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers["Accept-Language"] = "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2";
                    wc.Headers["Content-Type"] = "application/json";
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/537.36";

                    if (parameter == null)
                    {
                        result = Encoding.UTF8.GetString(wc.UploadData(url, "POST", new byte[] { }));
                       
                    }
                    else
                    {
                        result = Encoding.UTF8.GetString(wc.UploadData(url, "POST", Encoding.UTF8.GetBytes(parameter)));
                    }
                    t_result = JsonConvert.DeserializeObject<T>(result);
                }
                catch (Exception ex)
                {
                    t_result = default(T);
                    ex.Message.SendLog_Exception();
                    $"【error】{url}|{parameter}".SendLog_Debug();
                    result.SendLog_Debug();
                }
            }
            return t_result;
        }

        public static T Post<T>(string url, object parameter)
        {
            string result = string.Empty;
            T t_result = default(T);
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers["Accept-Language"] = "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2";
                    wc.Headers["Content-Type"] = "application/json";
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/537.36";

                    if (parameter == null)
                    {
                        result = Encoding.UTF8.GetString(wc.UploadData(url, "POST", new byte[] { }));

                    }
                    else
                    {
                        result = Encoding.UTF8.GetString(wc.UploadData(url, "POST", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(parameter))));
                    }
                    t_result = JsonConvert.DeserializeObject<T>(result);
                }
                catch (Exception ex)
                {
                    t_result = default(T);
                    ex.Message.SendLog_Exception();
                    $"【error】{url}|{parameter}".SendLog_Debug();
                    result.SendLog_Debug();
                }
            }
            return t_result;
        }
        public static string Get(string url)
        {
            string result = string.Empty;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers["Accept-Language"] = "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2";
                    wc.Headers["Content-Type"] = "application/json";
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/537.36";
                    result = Encoding.UTF8.GetString(wc.DownloadData(url));
                }
                catch (Exception ex)
                {
                    result = null;
                    ex.Message.SendLog_Exception();
                }
            }
            return result;
        }

        public static T Get<T>(string url)
        {
            string result = string.Empty;
            T t_result = default(T);
            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.Headers["Accept-Language"] = "zh-CN,zh;q=0.8,en;q=0.6,ja;q=0.4,zh-TW;q=0.2";
                    wc.Headers["Content-Type"] = "application/json";
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/537.36";
                    result = Encoding.UTF8.GetString(wc.DownloadData(url));
                    t_result = JsonConvert.DeserializeObject<T>(result);
                }
                catch (Exception ex)
                {
                    t_result = default(T);
                    ex.Message.SendLog_Exception();
                    $"【error】{url}".SendLog_Debug();
                    result.SendLog_Debug();
                }
             
            }
            return t_result;
        }
    }
}
