using Q.Lib.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Utility
{
    public static class Shell
    {
        public static Action<string> Action_Output = (s)=> { s.SendLog(); };
        public static Action<string> Action_Error = (s) => { s.SendLog_Exception(); };
        public static ShellResult RunCommand(string cmdStr, string workDir = null, bool redirect = true)
        {
            string fileName = cmdStr.Split(' ')[0];
            string arguments = cmdStr.Substring(fileName.Length);
            StringBuilder sb_output = new StringBuilder();
            StringBuilder sb_error = new StringBuilder();

            if (string.IsNullOrEmpty(workDir))
            {
                workDir = AppDomain.CurrentDomain.BaseDirectory;
            }
            using (var bash = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = redirect,
                    RedirectStandardError = redirect,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    WorkingDirectory = workDir
                }
            })
            {
                if (redirect)
                {
                    bash.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                    bash.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                }


                    bash.Start();
                Action_Output?.Invoke($"{fileName} {arguments}");
                if (redirect)
                {
                    bash.OutputDataReceived += (s, e) => { Action_Output?.Invoke(e.Data); sb_output.AppendLine(e.Data); };
                    bash.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) { Action_Error?.Invoke(e.Data); sb_error.AppendLine(e.Data); } };

                    bash.BeginOutputReadLine();
                    bash.BeginErrorReadLine();
                }

                bash.WaitForExit();
                int ExitCode = bash.ExitCode;
                bash.Close();

                if (redirect)
                    return new ShellResult(sb_output.ToString(), sb_error.ToString(), ExitCode);
                else
                    return new ShellResult(null, null, ExitCode);
            }
        }
    }
}
