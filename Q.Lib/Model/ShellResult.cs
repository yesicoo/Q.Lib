using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib.Model
{
    public class ShellResult
    {
        public string OutPut {internal set; get; }
        public string Error { internal set; get; }
        public int ExitCode { internal set; get; }

        public ShellResult(string outPut, string error, int exitCode)
        {
            this.OutPut = outPut;
            this.Error = error;
            this.ExitCode = exitCode;
        }

        public ShellResult InheritedOther(ShellResult sr)
        {
            this.OutPut = $"{sr.OutPut}{Environment.NewLine}{this.OutPut}";
            this.Error = $"{sr.Error}{Environment.NewLine}{this.Error}";
            return this;
        }

        public ShellResult AppendOutput(string text)
        {
            this.OutPut += (Environment.NewLine + text);
            return this;
        }

        public ShellResult AppendErrMsg(string text)
        {
            this.Error += (Environment.NewLine + text);
            return this;
        }
    }
}
