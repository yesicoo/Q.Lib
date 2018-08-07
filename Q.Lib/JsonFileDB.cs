using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib
{
    public static class JsonFileDB
    {
        public static bool SaveObject(string fileName, object o)
        {
            if (o == null)
            {
                return false;
            }
            try
            {
                string dirPath = AppDomain.CurrentDomain.BaseDirectory + "/JsonFiles/";
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(o);
                using (StreamWriter sw = File.CreateText(dirPath + fileName + ".jf"))
                {
                    sw.Write(jsonStr.ToString());
                }
            }
            catch (Exception ex)
            {
                ex.ToString().SendLog_Exception();
                return false;
            }
            return true;
        }
        public static T ReadObject<T>(string fileName)
        {
            string dirPath = AppDomain.CurrentDomain.BaseDirectory + "/JsonFiles/";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                return default(T);
            }
            else
            {
                try
                {
                    if (!File.Exists(dirPath + fileName + ".jf"))
                    {
                        return default(T);
                    }
                    else
                    {
                        string jsonStr = File.ReadAllText(dirPath + fileName + ".jf");

                        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonStr);
                    }
                }
                catch (Exception ex)
                {
                    ex.ToString().SendLog_Exception();
                    return default(T);
                }
            }
        }
    }
}
