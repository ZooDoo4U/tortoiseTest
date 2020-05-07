using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BittrexLoggerLibrary;

namespace BittrexLogger
{
    class Program
    {
        //
        //  Sample test to get logging working...
        //
        static void Main( string[] args )
        {
            // BittrexLoggerLibrary.K\ log = new Logger();

            Logger.SetLogFileName(Path.Combine(System.Environment.CurrentDirectory, "testLog.txt"));
            Logger.Log("foo", System.Diagnostics.TraceEventType.Verbose);
            


        }
    }
}
