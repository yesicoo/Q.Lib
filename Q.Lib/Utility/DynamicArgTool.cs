using Q.Lib.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Utility
{
    public static class DynamicArgTool
    {
        public static T TryGetValue<T>(dynamic item, T defaultValue = default(T))
        {
            try
            {
                return (T)Convert.ChangeType(item, typeof(T));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
        public static T TryGetObj<T>(dynamic item, T defaultValue = default(T))
        {
            try
            {
                return Json.Convert2T<T>(item);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static string TryGetString(dynamic item, string defaultValue = null)
        {
            try
            {
                return (string)Convert.ChangeType(item, typeof(string));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
        public static int TryGetInt(dynamic item, int defaultValue = 0)
        {
            try
            {
                return (int)Convert.ChangeType(item, typeof(int));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static long TryGetLong(dynamic item, long defaultValue = 0)
        {
            try
            {
                return (long)Convert.ChangeType(item, typeof(long));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
        public static DateTime TryGetDateTime(dynamic item, DateTime defaultValue)
        {
            try
            {
                return (DateTime)Convert.ChangeType(item, typeof(DateTime));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        internal static T[] TryGetArray<T>(dynamic item, T[] defaultValue = null)
        {
            try
            {

                return Json.Convert2T<T[]>(item);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
        internal static List<T> TryGetList<T>(dynamic item, List<T> defaultValue = null)
        {
            try
            {
                return Json.Convert2T<List<T>>(item);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
