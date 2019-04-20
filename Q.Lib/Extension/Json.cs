﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Extension
{
    public static class Json
    {
        public static string ToJsonStr(this object o,bool pretty = false)
        {
            if (o == default(object))
            {
                return null;
            }
            else
            {
                if (pretty)
                {
                    return JsonConvert.SerializeObject(o,Formatting.Indented);
                }
                else
                {
                    return JsonConvert.SerializeObject(o);
                }
            }
        }

        public static T ToObj<T>(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return default(T);
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(str);
            }
        }
    }
}
