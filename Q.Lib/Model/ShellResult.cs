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
    }
}
