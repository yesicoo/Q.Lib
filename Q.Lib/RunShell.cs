using Q.Lib.Model;
using System;

namespace Q.Lib
{
    public class RunShell
    {
        public static Shell Start(string pName)
        {
            return new Shell(pName);
        }
    }
}
