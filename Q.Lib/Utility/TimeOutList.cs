using Q.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Q.Lib.Utility
{
    public class TimeOutList<T>
    {

        List<CacheItem<T>> _Items = new List<CacheItem<T>>();

        int _ClearMin = 0;
        /// <summary>
        /// 默认5分钟后清理
        /// </summary>
        /// <param name="min"></param>
        public TimeOutList(int min = 5)
        {
            _ClearMin = min;
            QCrontab.RunWithSecond(60, () => ClearOldData(), typeof(T) + "缓存对象清理-TimeOutList");
        }
        private void ClearOldData()
        {
            var ClearTime = DateTime.Now.AddMinutes(-_ClearMin);
            if (_Items.Exists(x => x.Expire < ClearTime))
            {
                lock (_Items)
                {
                    _Items.RemoveAll(x => x.Expire < ClearTime);
                }
            }
        }
        public bool Add(T t)
        {
            if (_Items.Exists(x => x.Item.Equals(t)))
            {
                return false;
            }
            else
            {
                lock (_Items)
                {
                    _Items.Add(new CacheItem<T> { Item = t, Expire = DateTime.Now });
                }
                return true;
            }
        }
        public bool Exists(T t)
        {
            return _Items.Exists(x => x.Item.Equals(t));
        }

        public void Remove(T t)
        {
            lock (_Items)
            {
                _Items.RemoveAll(x => x.Item.Equals(t));
            }
        }

        public void CleareAll()
        {
            lock (_Items)
            {
                _Items.Clear();
            }
        }
    }
}
