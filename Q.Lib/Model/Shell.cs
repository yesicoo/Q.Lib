using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Model
{
    public class Shell
    {
        private string pName;
        private string Arguments = "";

        public Shell(string pName)
        {
            this.pName = pName;
        }

        public Shell AddArguments(string p)
        {
            Arguments += " " + p.Trim();
            return this;
        }

        public string Run()
        {
            var result_Str = "";
            var escapedArgs = Arguments.Replace("\"", "\\\"");
            try
            {

                var startInfo = new ProcessStartInfo
                {
                    FileName = pName,
                    Arguments = escapedArgs,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = false
                };



                Process p = new Process() { StartInfo = startInfo };
                if (p.Start())
                {
                    result_Str = p.StandardOutput.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray()) + p.StandardError.ReadToEnd().TrimEnd(Environment.NewLine.ToCharArray());
                    p.WaitForExit();
                    p.Close();
                }
                QLog.SendLog(pName + escapedArgs);
                QLog.SendLog(result_Str);
                return result_Str;
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
        }


    }
}
