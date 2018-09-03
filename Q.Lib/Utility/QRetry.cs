using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib
{
    public static class QRetry
    {
        /// <summary>
        /// 自定义异常重试
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="time"></param>
        /// <param name="wait"></param>
        /// <param name="action"></param>
        /// <param name="retryInfo"></param>
        public static void HandException<E>(int time, TimeSpan wait, Action action, Action<E, int, TimeSpan> retryInfo = null) where E : Exception
        {
            var task = Task.Run(() =>
            {
                for (int i = 0; i < (time + 1); i++)
                {
                    try
                    {
                        action();
                        break;
                    }
                    catch (E e)
                    {
                        if (i == time)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e, i + 1, wait);
                        Thread.Sleep(wait);
                    }
                }

            });

        }
        /// <summary>
        /// 自定义异常重试（有返回，异常返回默认值）
        /// </summary>
        /// <typeparam name="TResult">返回结果</typeparam>
        /// <param name="time">次数</param>
        /// <param name="wait">等待时间</param>
        /// <param name="action">动作</param>
        /// <param name="retryInfo">重试信息</param>
        /// <returns></returns>
        public static TResult HandException<E, TResult>(int time, TimeSpan wait, Func<TResult> action, Action<E, int, TimeSpan> retryInfo = null) where E : Exception
        {
            return Task.Run(() =>
            {
                TResult result = default(TResult);
                for (int i = 0; i < (time + 1); i++)
                {
                    try
                    {
                        result = action();
                        break;
                    }
                    catch (E e)
                    {
                        if (i == time)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e, i + 1, wait);
                        Thread.Sleep(wait);
                    }
                }
                return result;
            }).Result;
        }
        /// <summary>
        /// 自定义异常重试（有返回，设置异常返回值）
        /// </summary>
        /// <typeparam name="TResult">返回结果</typeparam>
        /// <param name="time">次数</param>
        /// <param name="wait">每次等待时间</param>
        /// <param name="failedValue">失败返回值</param>
        /// <param name="func">执行方法</param>
        /// <param name="retryInfo">重试信息（Exception,Time,TimeSpan)</param>
        /// <returns></returns>
        public static TResult HandException<E, TResult>(int time, TimeSpan wait, TResult failedValue, Func<TResult> func, Action<E, int, TimeSpan> retryInfo = null) where E : Exception
        {
            return Task.Run(() =>
            {
                TResult result = failedValue;
                for (int i = 0; i < (time + 1); i++)
                {
                    try
                    {
                        result = func();
                        break;
                    }
                    catch (E e)
                    {
                        if (i == time)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e, i + 1, wait);
                        Thread.Sleep(wait);
                    }
                }
                return result;
            }).Result;
        }
        /// <summary>
        /// 自定义异常重试
        /// </summary>
        /// <param name="waits">重试间隔</param>
        /// <param name="action">运行方法</param>
        /// <param name="retryInfo">重试信息</param>
        public static void HandException<E>(TimeSpan[] waits, Action action, Action<E, int, TimeSpan> retryInfo = null) where E : Exception
        {
            var task = Task.Run(() =>
            {
                for (int i = 0; i < (waits.Length + 1); i++)
                {
                    try
                    {
                        action();
                        break;
                    }
                    catch (E e)
                    {
                        if (i == waits.Length)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e, i + 1, waits[i]);
                        Thread.Sleep(waits[i]);
                    }
                }

            });
        }
        /// <summary>
        /// 自定义异常重试 （有返回，异常返回默认值）
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="waits"></param>
        /// <param name="func"></param>
        /// <param name="retryInfo"></param>
        /// <returns></returns>
        public static TResult HandException<E, TResult>(TimeSpan[] waits, Func<TResult> func, Action<E, int, TimeSpan> retryInfo = null) where E : Exception
        {
            return Task.Run(() =>
            {
                TResult result = default(TResult);
                for (int i = 0; i < (waits.Length + 1); i++)
                {
                    try
                    {
                        result = func();
                        break;
                    }
                    catch (E e)
                    {
                        if (i == waits.Length)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e, i + 1, waits[i]);
                        Thread.Sleep(waits[i]);
                    }
                }
                return result;
            }).Result;
        }

        /// <summary>
        /// 自定义异常重试 （有返回，设置异常返回值）
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="waits"></param>
        /// <param name="func"></param>
        /// <param name="retryInfo"></param>
        /// <returns></returns>
        public static TResult HandException<E, TResult>(TimeSpan[] waits, TResult failedValue, Func<TResult> func, Action<E, int, TimeSpan> retryInfo = null) where E : Exception
        {
            return Task.Run(() =>
            {
                TResult result = failedValue;
                for (int i = 0; i < (waits.Length + 1); i++)
                {
                    try
                    {
                        result = func();
                        break;
                    }
                    catch (E e)
                    {
                        if (i == waits.Length)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e, i + 1, waits[i]);
                        Thread.Sleep(waits[i]);
                    }
                }
                return result;
            }).Result;
        }

        /// <summary>
        /// 结果判断重试（异常也重试）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="time"></param>
        /// <param name="wait"></param>
        /// <param name="func"></param>
        /// <param name="retryInfo"></param>
        /// <returns></returns>
        public static T HandResult<T>(Func<T, bool> exp, int time, TimeSpan wait, Func<T> func, Action<string, int, TimeSpan> retryInfo = null)
        {
           
            return Task.Run(() =>
            {
                var result = default(T);
                for (int i = 0; i < (time + 1); i++)
                {
                    try
                    {
                        result = func();
                        if (i == time)
                        {
                            break;
                        }
                        else if (exp.Invoke(result))
                        {
                            retryInfo?.Invoke("结果命中约束条件", i + 1, wait);
                            Thread.Sleep(wait);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        if (i == time)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e.Message, i + 1, wait);
                        Thread.Sleep(wait);
                    }
                }
                return result;
            }).Result;

        }

        /// <summary>
        /// 结果判断重试（异常也重试）
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="expression">判断表达式</param>
        /// <param name="waits">每次重试间隔</param>
        /// <param name="func">执行方法</param>
        /// <param name="retryInfo">重试信息</param>
        /// <returns></returns>
        public static T HandResult<T>(Expression<Func<T, bool>> expression, TimeSpan[] waits, Func<T> func, Action<string, int, TimeSpan> retryInfo = null)
        {
            var exp = expression.Compile();
            return Task.Run(() =>
            {
                var result = default(T);
                for (int i = 0; i < (waits.Length + 1); i++)
                {
                    try
                    {
                        result = func();
                        if (i == waits.Length)
                        {

                            break;
                        }
                        else if (exp.Invoke(result))
                        {
                            retryInfo?.Invoke("结果命中约束条件", i + 1, waits[i]);
                            Thread.Sleep(waits[i]);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        if (i == waits.Length)
                        {
                            break;
                        }
                        retryInfo?.Invoke(e.Message, i + 1, waits[i]);
                        Thread.Sleep(waits[i]);
                    }
                }
                return result;
            }).Result;
        }
    }
}
