using Q.Lib.Model;
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
    public class TimeOutDic<T>
    {
        ConcurrentDictionary<string, CacheItem<T>> _Items = new ConcurrentDictionary<string, CacheItem<T>>();
        Func<string, T> _Func = null;
        int _ClearMin = 0;
        public TimeOutDic( Func<string, T> func, int min = 5)
        {
            _Func = func;
            _ClearMin = min;
            QCrontab.RunWithSecond(60, () => ClearOldData(), typeof(T) + "缓存对象清理-LazyCache");
        }

        private void ClearOldData()
        {
            var keys = _Items.Where(x => x.Value.Expire < DateTime.Now.AddMinutes(-_ClearMin)).Select(x => x.Key).ToList();
            foreach (var key in keys)
            {
                _Items.TryRemove(key, out var value);
            }
        }

        /// <summary>
        /// 添加缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="t"></param>
        public void AddValue(string key, T t)
        {
            _Items.TryAdd(key, new CacheItem<T> { Item = t, Expire = DateTime.Now });
        }
        /// <summary>
        /// 获取缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetValue(string key)
        {
            if (_Items.TryGetValue(key, out var value))
            {
                return value.Item;
            }
            else
            {
                if (_Func != null)
                {
                    var item = _Func(key);
                    if (item != null)
                    {
                        _Items.TryAdd(key, new CacheItem<T> { Item = item, Expire = DateTime.Now });
                        return item;
                    }
                    else
                    {
                        return default;
                    }
                }
                else
                {
                    return default;
                }
            }
        }

        public T GetValueAfterRemove(string key)
        {
            if (_Items.TryRemove(key, out var value))
            {
                return value.Item;
            }
            else
            {

                return default;

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
