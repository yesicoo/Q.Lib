using Q.Lib.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Q.Lib.Utility
{
    internal class TimeOutGroupList<T>
    {
        ConcurrentDictionary<string, List<CacheItem<T>>> _GroupItems = new ConcurrentDictionary<string, List<CacheItem<T>>>();  

        int _ClearMin = 0;
        /// <summary>
        /// 默认5分钟后清理
        /// </summary>
        /// <param name="min"></param>
        public TimeOutGroupList(int min = 5)
        {
            _ClearMin = min;
            QCrontab.RunWithSecond(60, () => ClearOldData(), typeof(T) + "缓存对象清理-TimeOutGroupList");
        }
        private void ClearOldData()
        {
            var ClearTime = DateTime.Now.AddMinutes(-_ClearMin);
            foreach (var item in _GroupItems)
            {
                if (item.Value.Exists(x => x.Expire < ClearTime))
                {
                    lock (item.Value)
                    {
                        item.Value.RemoveAll(x => x.Expire < ClearTime);
                    }
                }

            }
            
        }

        public bool Add(string key,T t)
        {
            if (!_GroupItems.TryGetValue(key, out var value))
            {
                value = new List<CacheItem<T>>();
                _GroupItems.TryAdd(key, value);
            }
            if (value.Exists(x => x.Item.Equals(t)))
            {
                return false;
            }
            else
            {
                lock (value)
                {
                    value.Add(new CacheItem<T> { Item = t, Expire = DateTime.Now });
                }
                return true;
            }
        }
        public bool Exists(string key,T t)
        {
            if (!_GroupItems.TryGetValue(key, out var value))
            {
                value = new List<CacheItem<T>>();
                _GroupItems.TryAdd(key, value);
            }
            return value.Exists(x => x.Item.Equals(t));
        }

        public void Remove(string key,T t)
        {
            if (!_GroupItems.TryGetValue(key, out var value))
            {
                value = new List<CacheItem<T>>();
                _GroupItems.TryAdd(key, value);
            }
            lock (value)
            {
                value.RemoveAll(x => x.Item.Equals(t));
            }
        }

        public void CleareAll(string key=null)
        {
            if (key == null)
            {
                _GroupItems.Clear();
            }
            else
            {
                if (!_GroupItems.TryGetValue(key, out var value))
                {
                    value = new List<CacheItem<T>>();
                    _GroupItems.TryAdd(key, value);
                }
                lock (value)
                {
                    value.Clear();
                }
            }
           
        }
    }
}
