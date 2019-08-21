using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Extension
{
    public static class IDictionary
    {
        public static T GetValue<T>(this IDictionary<object, object> dic, object key)
        {
            if (dic.ContainsKey(key))
            {
                return (T)dic[key];
            }
            else
            {
                return default(T);
            }
        }

        public static bool TryGetValue<T>(this IDictionary<object, object> dic, object key, out T t)
        {
            if (dic.ContainsKey(key))
            {
                t = (T)dic[key];
                return true;
            }
            else
            {
                t = default(T);
                return false;
            }
        }
    }
}
