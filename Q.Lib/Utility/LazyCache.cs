﻿using Q.Lib.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Utility
{
    /// <summary>
    /// 懒加载对象键值缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LazyCache<T> where T:new()
    {
        ConcurrentDictionary<string, LazyCacheItem<T>> _Items = new ConcurrentDictionary<string, LazyCacheItem<T>>();
        Func<string, T> _Func = null;
        int _ClearMin = 0;
        public LazyCache(int min, Func<string,T> func)
        {
            _Func = func;
            _ClearMin = min;
            QCrontab.RunWithSecond(60, () => ClearOldData(), typeof(T)+"缓存对象清理");
        }

        private void ClearOldData()
        {
           var keys= _Items.Where(x => x.Value.Expire < DateTime.Now.AddMinutes(-_ClearMin)).Select(x=>x.Key).ToList();
            foreach (var key in keys)
            {
                _Items.TryRemove(key, out var value);
            }
        }

        public T GetValue(string key)
        {
            if(_Items.TryGetValue(key,out var value))
            {
                return value.Item;
            }
            else
            {
                var item = _Func(key);
                if (item != null)
                {
                    _Items.TryAdd(key, new LazyCacheItem<T> { Item = item, Expire = DateTime.Now });
                    return item;
                }
                else
                {
                    return default;
                }
            }
        }

        public bool Clear(string key)
        {
            return _Items.TryRemove(key, out var value);
        }

        public bool ClearALL()
        {
             _Items.Clear();
            return true;
        }
    }
}