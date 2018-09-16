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

        public static string RunWithSecond(int second, Action action, string name = "")
        {
            string id = QTools.GuidStr();
            QCrontabJob qcj = new QCrontabJob();
            qcj.action = action;
            qcj.RunMode = 1;
            qcj.Second = second;
            qcj.RemainderSecond = (DateTime.Now.Ticks / 10000000) % second;
            actions.TryAdd(id, qcj);
            QLog.SendLog($"添加定时任务 {name}({id})： Loop  {second} Second");
            return id;
        }

        public static string RunOnceWithTime(DateTime dateTime, Action action, string name = "")
        {
            if (dateTime < DateTime.Now)
            {
                action();
                return "";
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.action = action;
                qcj.RunMode = 0;
                qcj.Second = (dateTime.Ticks / 10000000);
                QLog.SendLog($"添加定时任务 {name}({id})： OnceTime {dateTime}");
                actions.TryAdd(id, qcj);
                return id;
            }
        }
        #region RunRepeatWithTimePoint
        /// <summary>
        /// 
        /// </summary>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public static string RunRepeatWithTimePoint(int second, Action action, string name = "")
        {
            if (second < 0 || second > 59)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})： EverySecond {second}");
                return id;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="min">分</param>
        /// <param name="second">秒</param>
        /// <param name="action">执行方法</param>
        /// <returns></returns>
        public static string RunRepeatWithTimePoint(int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{min}:{second} EveryHours");
                return id;
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
        public static string RunRepeatWithTimePoint(int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{hour}:{min}:{second} EveryDays");
                return id;
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
        public static string RunRepeatWithTimePoint(int day, int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || day < 1 || day > 31)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SDay = day;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{hour} {hour}:{min}:{second} EveryMonths");
                return id;
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
        public static string RunRepeatWithWeek(int week, int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || week < 0 || week > 6)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.action = action;
                qcj.RunMode = 3;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SWeek = week;
                QLog.SendLog($"添加定时任务 {name}({id})：{week} {hour}:{min}:{second} EveryWeeks");
                actions.TryAdd(id, qcj);
                return id;
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
        public static string RunRepeatWithTimePoint(int month, int day, int hour, int min, int second, Action action, string name = "")
        {
            if (second < 0 || second > 59 || min < 0 || min > 59 || hour < 0 || hour > 23 || day < 1 || day > 31 || month < 1 || month > 12)
            {
                return "Error:时间点无效";
            }
            else
            {
                string id = QTools.GuidStr();
                QCrontabJob qcj = new QCrontabJob();
                qcj.action = action;
                qcj.RunMode = 2;
                qcj.SSecond = second;
                qcj.SMinute = min;
                qcj.SHour = hour;
                qcj.SDay = day;
                qcj.SMonth = month;
                actions.TryAdd(id, qcj);
                QLog.SendLog($"添加定时任务 {name}({id})：{month}-{day} {hour}:{min}:{second} EveryYears");
                return id;
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
            actions.AsParallel().ForAll(x =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (x.Value.RunMode == 1)
                        {
                            if (t % x.Value.Second == x.Value.RemainderSecond)
                            {
                                QLog.SendLog_Debug("Run", x.Key);

                                try
                                {
                                    x.Value.action();
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }

                            }
                        }
                        else if (x.Value.RunMode == 0)
                        {
                            if (t == x.Value.Second)
                            {
                                QLog.SendLog_Debug("Run", x.Key);

                                try
                                {
                                    x.Value.action();
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }

                                actions.TryRemove(x.Key, out QCrontabJob job);
                            }
                        }
                        else if (x.Value.RunMode == 2)
                        {
                            var now = DateTime.Now;
                            if ((x.Value.SSecond == -1 || now.Second == x.Value.SSecond) &&
                                (x.Value.SMinute == -1 || now.Minute == x.Value.SMinute) &&
                                (x.Value.SHour == -1 || now.Hour == x.Value.SHour) &&
                                (x.Value.SDay == -1 || now.Day == x.Value.SDay) &&
                                (x.Value.SMonth == -1 || now.Month == x.Value.SMonth))
                            {
                                QLog.SendLog_Debug("Run", x.Key);

                                try
                                {
                                    x.Value.action();
                                }
                                catch (Exception ex)
                                {
                                    ex.ToString().SendLog_Exception();
                                }

                            }

                        }
                        else if (x.Value.RunMode == 3)
                        {
                            var now = DateTime.Now;
                            if ((x.Value.SSecond == -1 || now.Second == x.Value.SSecond) &&
                                (x.Value.SMinute == -1 || now.Minute == x.Value.SMinute) &&
                                (x.Value.SHour == -1 || now.Hour == x.Value.SHour) &&
                                (x.Value.SWeek == -1 || (int)now.DayOfWeek == x.Value.SWeek))
                            {
                                QLog.SendLog_Debug("Run", x.Key);

                                try
                                {
                                    x.Value.action();
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
    internal class QCrontabJob
    {
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
