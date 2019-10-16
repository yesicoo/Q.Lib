using Q.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Utility
{
    internal class ObjectPool<T> where T : new()
    {
        //已使用记录
        private List<int> usedRecord;
        //未使用记录
        private List<int> unUsedRecord;
        //池子
        private List<PoolObj<T>> pool;
        //池子最大容量
        private int capacity;

        private void init()
        {
            this.pool = new List<PoolObj<T>>(this.capacity);
            this.usedRecord = new List<int>(this.capacity);
            this.unUsedRecord = new List<int>(this.capacity);
            for (int i = 0; i < this.capacity; i++)
            {
                this.unUsedRecord.Add(i);
                this.pool.Add(new PoolObj<T>(i));
            }
        }

        /**获取可使用数量**/
        public int GetUsedCount()
        {
            return this.capacity - this.usedRecord.Count;
        }

        public PoolObj<T> Pop()
        {
            int index = 0;
            lock (this)
            {
                if (GetUsedCount() <= 0)
                {
                    extCapacity();
                }
                index = this.unUsedRecord[0];
                this.unUsedRecord.RemoveAt(0);
                this.usedRecord.Add(index);
                return this.pool[index];
            }
        }

        public void Push(PoolObj<T> args)
        {
            int index = 0;
            lock (this)
            {
                index = args.Index;
                this.unUsedRecord.Add(index);
                this.usedRecord.Remove(index);
            }
        }

        /** 扩展容量   */
        private void extCapacity()
        {
            int minNewCapacity = 200;
            int newCapacity = Math.Min(this.capacity, minNewCapacity);

            //每次以minNewCapacity倍数扩展
            if (newCapacity > minNewCapacity)
            {
                newCapacity += minNewCapacity;
            }
            else
            {
                //以自身双倍扩展空间
                newCapacity = 64;
                while (newCapacity < minNewCapacity)
                {
                    newCapacity <<= 1;
                }
            }


            for (int i = this.capacity; i < newCapacity; i++)
            {
                this.unUsedRecord.Add(i);
                this.pool.Add(new PoolObj<T>(i));
            }

            this.capacity = newCapacity;
        }


        //getter

        public int GetCapacity()
        {
            return this.capacity;
        }
    }
}
