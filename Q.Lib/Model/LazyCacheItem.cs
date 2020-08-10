using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Model
{
   internal class LazyCacheItem<T>
    {
        public T Item { set; get; }
        public DateTime Expire { set; get; }
    }
}
