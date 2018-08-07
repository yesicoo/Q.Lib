using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Extension
{
   public static class Object
    {
        public static string ToJsonString(this object o)
        {
            if (o != null)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(o);
            }
            else
            {
                return null;
            }
        }
    }
}
