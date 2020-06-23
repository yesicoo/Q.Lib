using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Extension
{
   public static class QIEnumerable
    {
        public static bool TryFirstOrDefault<T>(this IEnumerable<T> ts, out T t)
        {
            var result = ts.FirstOrDefault();
            t = result;
            if (result != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool TryFirstOrDefault<T>(this IEnumerable<T> ts, Func<T, bool> predicate, out T t)
        {
            var result = ts.FirstOrDefault(predicate);
            t = result;
            if (result != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 循环每个元素进行指定操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ts"></param>
        /// <param name="action"></param>
        public static  void ForEach<T>(this IEnumerable<T> ts, Action<T> action)
        {
            foreach (var t in ts)
            {
                action(t);
            }
        }
    }
}
