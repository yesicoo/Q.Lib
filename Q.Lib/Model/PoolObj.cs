using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Model
{
    public class PoolObj<T> where T : new()
    {
        public int Index;
        public T Obj;

        public PoolObj(int index)
        {
            Index = index;
            Obj = new T();
        }
    }
}
