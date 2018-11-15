using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib
{
    public static class QCrontab
    {
        static Timer timer = new Timer(Run, null, 0, 1000);

        static ConcurrentDictionary<string, QCrontabJob> actions = new ConcurrentDictionary<string, QCrontabJob>();

        public static QCrontabJob RunWithSecond(int second, Action action, string name = "")
        {
            string id = QTools.GuidStr();
            QCrontabJob qcj = new QCrontabJob();
            qcj.ID = id;
            qcj.Name = name;
            qcj.action = action;
            qcj.RunMode = 1;
            qcj.Second = second;
            qcj.RemainderSecond = (DateTime.Now.Ticks / 10000000) % second;
            actions.TryAdd(id, qcj);
            QLog.SendLog($"添加定时任务 {name}({id})： Loop  {second} Second");
            return qcj;
        }

        public static QCrontabJob RunOnceWithTime(DateTime dateTime, Action action, string name = "")
        {
            if (dateTime < DateTime.Now)
            {
                action();
                return null;
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.ID = id;
                qcj.Name = name;
                qcj.action = action;
                qcj.RunMode = 0;
                qcj.Second = (dateTime.Ticks / 10000000);
                QLog.SendLog($"添加定时任务 {name}({id})： OnceTime {dateTime}");
                actions.TryAdd(id, qcj);
                return qcj;
            }
        }
        #region RunRepeatWithTimePoint
        /// <summary>
        /// 
        /// </summary>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public static QCrontabJob RunRepeatWithTimePoint(int second, Action action, string name = "")
        {
            if (second < 0 || second > 59)
            {
                 "Error:时间点无效".SendLog_Exception();
                return null;
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.ID = id;
                qcj.Name = name;
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})： EverySecond {second}");
                return qcj;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public static QCrontabJob RunRepeatWithTimePoint(int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59)
            {
                 "Error:时间点无效".SendLog_Exception();
                return null;
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.ID = id;
                qcj.Name = name;
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{min}:{second} EveryHours");
                return qcj;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hour">时</param>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public static QCrontabJob RunRepeatWithTimePoint(int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23)
            {
                 "Error:时间点无效".SendLog_Exception();
                return null;
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.ID = id;
                qcj.Name = name;
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{hour}:{min}:{second} EveryDays");
                return qcj;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="day">日</param>
        /// <param name="hour">时</param>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public static QCrontabJob RunRepeatWithTimePoint(int day, int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || day < 1 || day > 31)
            {
                "Error:时间点无效".SendLog_Exception();
                return null;
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.ID = id;
                qcj.Name = name;
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SDay = day;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{hour} {hour}:{min}:{second} EveryMonths");
                return qcj;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="week">周 0-6 0是周日</param>
        /// <param name="hour">时</param>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">方法</param>
        /// <returns></returns>
        public static QCrontabJob RunRepeatWithWeek(int week, int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || week < 0 || week > 6)
            {
                "Error:时间点无效".SendLog_Exception();
                return null;
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.ID = id;
                qcj.Name = name;
                qcj.action = action;
                qcj.RunMode = 3;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SWeek = week;
                QLog.SendLog($"添加定时任务 {name}({id})：{week} {hour}:{min}:{second} EveryWeeks");
                actions.TryAdd(id, qcj);
                return qcj;
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
        public static QCrontabJob RunRepeatWithTimePoint(int month, int day, int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || day < 1 || day > 31 || month < 1 || month > 12)
            {
                "Error:时间点无效".SendLog_Exception();
                return null;
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.ID = id;
                qcj.Name = name;
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SDay = day;
                qcj.SMonth = month;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{month}-{day} {hour}:{min}:{second} EveryYears");
                return qcj;
            }
        }
        #endregion


        public static void Stop()
        {
            actions.Clear();
            timer.Dispose();
        }


        private static void Run(object o)
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
                                    _x.Value.action();
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
                                    _x.Value.action();
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }

                                actions.TryRemove(_x.Key, out QCrontabJob job);
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
                                    _x.Value.action();
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
                                    _x.Value.action();
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
    public class QCrontabJob
    {
        public string ID;
        public string Name;
        public long Second;
        public long RemainderSecond;
        /// <summary>
        /// 1-反复运行 0-定点执行一次 2-定点执行多次 -3周循环
        /// </summary>
        public int RunMode;
        public Action action;

        public int SSecond = -1;
        public int SMinute = -1;
        public int SHour = -1;
        public int SDay = -1;
        public int SMonth = -1;
        public int SWeek = -1;
    }
}
