using Q.Lib.Extension;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib
{

    public class QCrontab<T> where T : IQCrontabJob
    {
        Timer timer = null;
        ConcurrentDictionary<string, QPersistenceCrontabJob<T>> actions = new ConcurrentDictionary<string, QPersistenceCrontabJob<T>>();

        public QCrontab()
        {
            if (timer == null)
            {
                timer = new Timer(Run, null, 0, 1000);
            }
        }

        public void Set(string config)
        {
            var runConfig = Json.ToObj<List<QPersistenceCrontabJob<T>>>(config);
            if (runConfig != null)
            {
                foreach (var item in runConfig)
                {
                    long t = (DateTime.Now.Ticks / 10000000);
                    if (item.RunMode == 0 && item.Second < t)
                    {
                        QLog.SendLog($"载入的一次性定时任务：{item.Name}({item.ID}) 已过期，取消载入！");
                    }
                    else
                    {
                        actions.TryAdd(item.ID, item);
                        QLog.SendLog($"载入的定时任务：{item.Name}({item.ID}) 成功！");
                    }
                }
            }

            if (timer == null)
            {
                timer = new Timer(Run, null, 0, 1000);
            }
        }
        /// <summary>
        /// 按照秒间隔循环执行
        /// </summary>
        /// <param name="second"></param>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string RunWithSecond(int second, T t, string name = "")
        {
            string id = QTools.GuidStr();
            QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
            qcj.ID = id;
            qcj.Name = name;
            qcj.RunMode = 1;
            qcj.Second = second;
            qcj.RemainderSecond = (DateTime.Now.Ticks / 10000000) % second;
            qcj.Argument = t;
            actions.TryAdd(id, qcj);
            QLog.SendLog($"添加带参定时任务 {name}({id})： Loop  {second} Second");
            return qcj.ID;
        }

        /// <summary>
        /// 定时执行一次
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="t"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string RunOnceWithTime(DateTime dateTime, T t, string name = "")
        {
            if (dateTime < DateTime.Now)
            {
                return "Error:任务已过期";
            }
            else
            {
                string id = QTools.GuidStr();
                QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
                qcj.ID = id;
                qcj.Name = name;
                qcj.Argument = t;
                qcj.RunMode = 0;
                qcj.Second = (dateTime.Ticks / 10000000);
                QLog.SendLog($"添加带参定时任务 {name}({id})： OnceTime {dateTime}");
                actions.TryAdd(id, qcj);
                return qcj.ID;
            }
        }
        #region RunRepeatWithTimePoint
        /// <summary>
        /// 每个分钟的某个秒时刻执行
        /// </summary>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public string RunRepeatWithTimePoint(int second, T t, string name = "")
        {
            if (second < 0 || second > 59)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
                qcj.ID = id;
                qcj.Name = name;
                qcj.Argument = t;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加带参定时任务 {name}({id})： EverySecond {second}");
                return qcj.ID;
            }
        }
        /// <summary>
        /// 每个小时的指定某分某秒执行
        /// </summary>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public string RunRepeatWithTimePoint(int min, int second, T t, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
                qcj.ID = id;
                qcj.Name = name;
                qcj.Argument = t;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加带参定时任务 {name}({id})：{min}:{second} EveryHours");
                return qcj.ID;
            }
        }
        /// <summary>
        /// 每天的某个小时某分某秒执行
        /// </summary>
        /// <param name="hour">时</param>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public string RunRepeatWithTimePoint(int hour, int min, int second, T t, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
                qcj.ID = id;
                qcj.Name = name;
                qcj.Argument = t;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加带参定时任务 {name}({id})：{hour}:{min}:{second} EveryDays");
                return qcj.ID;
            }
        }
        /// <summary>
        /// 每月的某天某个小时某分某秒执行
        /// </summary>
        /// <param name="day">日</param>
        /// <param name="hour">时</param>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public string RunRepeatWithTimePoint(int day, int hour, int min, int second, T t, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || day < 1 || day > 31)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
                qcj.ID = id;
                qcj.Name = name;
                qcj.Argument = t;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SDay = day;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加带参定时任务 {name}({id})：{hour} {hour}:{min}:{second} EveryMonths");
                return qcj.ID;
            }
        }
        /// <summary>
        /// 每周的某天某时某分某秒
        /// </summary>
        /// <param name="week">周 0-6 0是周日</param>
        /// <param name="hour">时</param>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">方法</param>
        /// <returns></returns>
        public string RunRepeatWithWeek(int week, int hour, int min, int second, T t, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || week < 0 || week > 6)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
                qcj.ID = id;
                qcj.Name = name;
                qcj.Argument = t;
                qcj.RunMode = 3;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SWeek = week;
                QLog.SendLog($"添加带参定时任务 {name}({id})：{week} {hour}:{min}:{second} EveryWeeks");
                actions.TryAdd(id, qcj);
                return qcj.ID;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="month">月</param>
        /// <param name="day">日</param>
        /// <param name="hour">时</param>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public string RunRepeatWithTimePoint(int month, int day, int hour, int min, int second, T t, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || day < 1 || day > 31 || month < 1 || month > 12)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QPersistenceCrontabJob<T> qcj = new QPersistenceCrontabJob<T>();
                qcj.ID = id;
                qcj.Name = name;
                qcj.Argument = t;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SDay = day;
                qcj.SMonth = month;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{month}-{day} {hour}:{min}:{second} EveryYears");
                return qcj.ID;
            }
        }
        #endregion


        public void Stop(Action<string> save)
        {
            timer.Dispose();
            save?.Invoke(actions.Values.ToJsonStr());
            actions.Clear();
            timer = null;
        }


        private void Run(object o)
        {
            long t = (DateTime.Now.Ticks / 10000000);
            actions.AsParallel().ForAll(_x =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (_x.Value.RunMode == 1)
                        {
                            if (t % _x.Value.Second == _x.Value.RemainderSecond)
                            {
                                QLog.SendLog_Debug("Run", $"{_x.Value.Name}({_x.Key})");
                                try
                                {

                                    _x.Value.Argument.Handle(_x.Value.Argument);
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }

                            }
                        }
                        else if (_x.Value.RunMode == 0)
                        {
                            if (t == _x.Value.Second)
                            {
                                QLog.SendLog_Debug("Run", $"{_x.Value.Name}({_x.Key})");

                                try
                                {
                                    _x.Value.Argument.Handle(_x.Value.Argument);
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }
                                actions.TryRemove(_x.Key, out QPersistenceCrontabJob<T> job);
                            }
                        }
                        else if (_x.Value.RunMode == 2)
                        {
                            var now = DateTime.Now;
                            if ((_x.Value.SSecond == -1 || now.Second == _x.Value.SSecond) &&
                                (_x.Value.SMinute == -1 || now.Minute == _x.Value.SMinute) &&
                                (_x.Value.SHour == -1 || now.Hour == _x.Value.SHour) &&
                                (_x.Value.SDay == -1 || now.Day == _x.Value.SDay) &&
                                (_x.Value.SMonth == -1 || now.Month == _x.Value.SMonth))
                            {
                                QLog.SendLog_Debug("Run", $"{_x.Value.Name}({_x.Key})");

                                try
                                {
                                    _x.Value.Argument.Handle(_x.Value.Argument);
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }
                            }

                        }
                        else if (_x.Value.RunMode == 3)
                        {
                            var now = DateTime.Now;
                            if ((_x.Value.SSecond == -1 || now.Second == _x.Value.SSecond) &&
                                (_x.Value.SMinute == -1 || now.Minute == _x.Value.SMinute) &&
                                (_x.Value.SHour == -1 || now.Hour == _x.Value.SHour) &&
                                (_x.Value.SWeek == -1 || (int)now.DayOfWeek == _x.Value.SWeek))
                            {
                                QLog.SendLog_Debug("Run", $"{_x.Value.Name}({_x.Key})");

                                try
                                {
                                    _x.Value.Argument.Handle(_x.Value.Argument);
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        QLog.SendLog_Exception(ex.ToString());
                    }
                });
            });


        }
    }
    internal class QPersistenceCrontabJob<T>
    {
        public string ID { set; get; }
        public string Name { set; get; }
        public long Second { set; get; }
        public long RemainderSecond { set; get; }
        /// <summary>
        /// 1-反复运行 0-定点执行一次 2-定点执行多次 -3周循环
        /// </summary>
        public int RunMode { set; get; }

        public int SSecond { set; get; } = -1;
        public int SMinute { set; get; } = -1;
        public int SHour { set; get; } = -1;
        public int SDay { set; get; } = -1;
        public int SMonth { set; get; } = -1;
        public int SWeek { set; get; } = -1;

        public T Argument { set; get; }
    }

    public interface IQCrontabJob
    {
        void Handle(IQCrontabJob data);

    }
}
