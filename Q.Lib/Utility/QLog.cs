using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Q.Lib
{
    public static class QLog
    {
        static ConcurrentQueue<string> q_log = new ConcurrentQueue<string>();
        static System.Timers.Timer t_ClearLog = new System.Timers.Timer(1000 * 60 * 10);
        static System.Timers.Timer t_Writelog = new System.Timers.Timer(1000);
        static int LogCycle = 0;
        static string DirPath = string.Empty;
        static string BasePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        static bool isRun = false; 
        public static Action<string> ExceptionLog;
        /// <summary>
        /// 日志级别
        /// 1-4 对应 Nomal,Exception,Debug,ShowOnly，向下包括
        /// 默认4，全部打印。 设置为0，不打印日志。
        /// </summary>
        public static int LogLevel = 4;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="path">日志文件目录（默认\log\）</param>
        /// <param name="logCycle">日志文件有效期（默认十五天）</param>
        public static void Init(string path = "/log/", int logCycle = 30)
        {
            DirPath = BasePath.TrimEnd('\\').TrimEnd('/') + path;
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(DirPath);
            di.Create();
            t_Writelog.Elapsed += t_Writelog_Elapsed;
            t_Writelog.Start();
            if (logCycle > 0)
            {
                LogCycle = logCycle;
                t_ClearLog.Elapsed += t_ClearLog_Elapsed;
                t_ClearLog.Start();
            }
            string sysInfo = $"";
            SendLog_Debug(string.Format("日志记录组件已启动(路径：{0} 清理周期{1}天)。", DirPath, logCycle));
        }
        /// <summary>
        /// 日志写入文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void t_Writelog_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (isRun) { return; }
            isRun = true;
            int i = 0;
            StringBuilder sb_log = new StringBuilder();
            //最多50条写一次 减少io操作
            while (q_log.Count > 0 && i < 50)
            {
                string log = string.Empty;
                if (q_log.TryDequeue(out log))
                {
                    sb_log.Append(log);
                }
            }
            using (StreamWriter sw = File.AppendText(DirPath + DateTime.Today.ToString("yy_MM_dd") + "_log.txt"))
            {
                sw.Write(sb_log.ToString());
            }
            isRun = false;
        }

        public static void Stop()
        {
            t_Writelog.Close();
            t_ClearLog.Close();
            StringBuilder sb_log = new StringBuilder();
            while (q_log.Count > 0)
            {
                string log = string.Empty;
                if (q_log.TryDequeue(out log))
                {
                    sb_log.Append(log);
                }
            }
            using (StreamWriter sw = File.AppendText(DirPath + DateTime.Today.ToString("yy_MM_dd") + "_log.txt"))
            {
                sw.Write(sb_log.ToString());
            }
            isRun = false;
        }
        /// <summary>
        /// 清理日志文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void t_ClearLog_Elapsed(object sender, ElapsedEventArgs e)
        {
            DirectoryInfo di = new DirectoryInfo(DirPath);
            foreach (FileInfo fi in di.GetFiles())
            {
                if (fi.CreationTime < DateTime.Now.AddDays(-LogCycle))
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {

                        SendLog("删除历史日志文件出错。\r\n" + ex.Message, E_LogType.Exception);
                    }
                }
            }
        }



        public static void SendLog(this string sLog, string tag, E_LogType logType = E_LogType.Nomal)
        {

            SendLog("[" + tag + "] " + sLog, logType);
        }
        public static void SendLog_Debug(this string sLog, string tag = "")
        {
            if (tag == "")
            {
                StackTrace trace = new StackTrace();
                StackFrame frame = trace.GetFrame(1);
                var method = frame.GetMethod();
                tag = method.ReflectedType.Name + "." + method.Name;
            }
            SendLog("[" + tag + "] " + sLog, E_LogType.Debug);
        }

        public static void SendLog_Exception(this string sLog, string tag = "")
        {
            if (tag == "")
            {
                StackTrace trace = new StackTrace();
                StackFrame frame = trace.GetFrame(1);
                var method = frame.GetMethod();
                tag = method.ReflectedType.Name + "." + method.Name;
            }
            SendLog("[" + tag + "] " + sLog, E_LogType.Exception);
        }




        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="sLog">日志内容</param>
        /// <param name="logType">日志类型（默认普通）</param>
        public static void SendLog(this string sLog, E_LogType logType = E_LogType.Nomal)
        {
            if ((int)logType > LogLevel)
            {
                return;
            }
            new Thread(() =>
            {
                StringBuilder log = new StringBuilder(DateTime.Now.ToString("HH:mm:ss"));
                switch (logType)
                {
                    case E_LogType.ShowOnly:
                        log.Append(" ").Append(sLog).AppendLine();
                        Console.Write(log);
                        break;
                    case E_LogType.Nomal:
                        log.Append(" ").Append(sLog).AppendLine();
                        Console.Write(log);
                        q_log.Enqueue(log.ToString());
                        break;
                    case E_LogType.Debug:
                        log.Append(" [Debug]").Append(sLog).AppendLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(log);
                        q_log.Enqueue(log.ToString());
                        break;
                    case E_LogType.Exception:
                        log.Append(" [Exception]").Append(sLog).AppendLine();
                        Console.ForegroundColor = ConsoleColor.Red;
                        var strlog = log.ToString();
                        Console.Write(strlog);
                        q_log.Enqueue(strlog);
                        ///发送通知
                        ExceptionLog?.Invoke(strlog);
                        break;
                }
                Console.ResetColor();

            }).Start();
        }


        public static void SendFileLog(this string sLog, string fileName)
        {
            using (StreamWriter sw = File.AppendText(DirPath + fileName + ".log"))
            {

                string log = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + sLog.ToString() + "\r\n";
                Console.WriteLine($"[{fileName}]" + log);
                sw.Write(log);
            }
        }
    }
    public enum E_LogType
    {
        /// <summary>
        /// 普通日志
        /// </summary>
        Nomal = 1,
        /// <summary>
        ///异常日志
        ///</summary>
        Exception = 2,
        /// <summary>
        ///调试日志
        ///</summary>
        Debug = 3,
        /// <summary>
        /// 仅显示(不保存到日志文件)
        /// </summary>
        ShowOnly = 4
    }
}
