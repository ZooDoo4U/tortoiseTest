using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;

namespace BittrexLoggerLibrary
{
    public static class Logger
    {
        //public enum  LogFileTypeEnum 
        //{
        //    FunParser,
        //    FunInstaller,
        //    Default,
        //}
        
        private static FileStream fs=null; 
        private static bool isFsOpen = false; 
        private static string _logFileName = String.Empty;
        private static TraceSource _traceSource ;
        public static readonly bool FlushAfterEveryWrite = true;
        public static string UserName;

        private static TraceSource traceSource 
        {
            get 
            {
                if( _traceSource == null  )
                {
                    _traceSource = CreateTraceSource(LoggerFileName, "BittrexLog", SourceLevels.Verbose);
                }
                return _traceSource; 
            }
        }        
        
        private static Stopwatch stopwatch = new Stopwatch();

        public static string LoggerFileName 
        {
            get
            {
                if( String.IsNullOrWhiteSpace(_logFileName))
                {
                    // TODO: This could be replaced by app.config settings.
                    _logFileName = $"FunParser_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{Logger.UserName}.log";
                }
                return _logFileName;
            }                       
        }

        public static void SetLogFileName( string logFilename) 
        {
            _logFileName = logFilename;
        }

        public static void ChangeLevel(SourceLevels level)
        {
            traceSource.Switch.Level = level;
        }

        public static void Flush()
        {
            traceSource.Flush();
        }

        public static void Close()
        {
            try
            {
                if (fs != null && isFsOpen == true)
                {
                    Logger.Log($"Closing Log File...", TraceEventType.Information);
                    Logger.Log($"Process has finished, you may not see a command prompt, press enter and you will.", TraceEventType.Resume);
                    traceSource.Flush();
                    fs.Flush();
                    fs.Close();
                    traceSource.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught:{ex.ToString()}");
            }
            finally
            {
                fs = null;
                isFsOpen=false;
            }
        }

        public static void StartTime(string text)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            Log(string.Format("StopWatch: {0} Time={1}ms", text, stopwatch.ElapsedMilliseconds));
        }

        public static void LogTime(string text)
        {
            Log(string.Format("{0} Time={1}ms", text, stopwatch.ElapsedMilliseconds));
        }

        public static void StopTime(string text)
        {
            stopwatch.Stop();
            Log(string.Format("{0} Time={1}ms", text, stopwatch.ElapsedMilliseconds));
        }

        public static void Log(string line)
        {
            Log(line, TraceEventType.Verbose, null);
        }

        public static void Log(string line, TraceEventType type)
        {
            Log(line, type, null);
        }

        public static void Log(string line, Exception exception)
        {
            Log(line, TraceEventType.Critical, exception);
        }

        public static SourceLevels TraceSource()
        {
            return traceSource.Switch.Level ;
        }

        public static void Disconnect()
        {
            Logger.Flush();
            _traceSource.Close();
            _traceSource = null;
            return;
        }
        
        private static string GetExceptionString(string line, Exception ex)
        {
            string totalLine = "";
            totalLine += line + "\n";
            totalLine += ex.Message + "\n";
            totalLine += ex.StackTrace + "\n";
            totalLine += ex.Source + "\n";
            if (ex.InnerException != null)
            {
                totalLine += ex.InnerException + "\n";
            }
            return totalLine;
        }

        private static void Log(string messageText, TraceEventType messageType, Exception exception)
        {
            if( !isFsOpen )
            {
                var x = Logger.traceSource;                
            }
            string reportedMsg = string.Empty;
            if (exception != null)
            {   
                reportedMsg = $"\n{messageText}\n{Utils.WalkExceptionChain(exception)}\n";                
                traceSource.TraceData(messageType, 1, reportedMsg, exception);
            }
            else
            {
                reportedMsg = messageText; 
                traceSource.TraceData(messageType, 1, reportedMsg);
            }
            
            if (FlushAfterEveryWrite)
            {
                Flush();
            }
        
            SetColor(messageType);
        
            if ( (int)traceSource.Switch.Level > (int)messageType || (int)traceSource.Switch.Level == -1 || 
                messageType == TraceEventType.Resume)
            {
                string msg = $"{messageType.ToString()}:";
                Console.WriteLine($"{msg,-15}{reportedMsg}");
            }

            Console.ResetColor();
        }

        private static void SetColor( TraceEventType messageType)
        {
            switch (messageType)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    break;
                case TraceEventType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case TraceEventType.Information:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.BackgroundColor = ConsoleColor.Black;
                    break;
                case TraceEventType.Verbose:
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case TraceEventType.Start:
                case TraceEventType.Stop:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    break;                
                case TraceEventType.Resume:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    break;
                case TraceEventType.Suspend:
                case TraceEventType.Transfer:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                default:
                    break;
            }

        }

        //
        //  Intended to allow output only to the screen when importing, not saved in the output log...
        //
        public static void Write( string textMessage,TraceEventType  messageType )
        {
            SetColor(messageType);
            
            if ( (int)traceSource.Switch.Level > (int)messageType || (int)traceSource.Switch.Level == -1 || 
                messageType == TraceEventType.Resume)
            {                
                Console.Write($"{textMessage}");
            }

            Console.ResetColor();
        }

        public static string WalkExceptionChain( Exception ex ) 
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append($"\n{ex.Message}");
            if( ex.InnerException != null ) 
            {
                sb.Append($"\n{WalkExceptionChain(ex.InnerException)}");
            }
            return sb.ToString();
        }


        private static object[] MakeNewArgs(string name, DateTime dateTime, params object[] args)
        {
            object[] newArgs;

            if (args == null)
            {
                newArgs = new object[2];
                newArgs[0] = name;
                newArgs[1] = dateTime.Ticks;
                return newArgs;
            }

            newArgs = new object[args.Length + 2];
            newArgs[0] = name;
            newArgs[1] = dateTime.Ticks;
            
            int count = 2;
            foreach (object o in args)
            {
                newArgs[count] = o;
                count++;
            }
            return newArgs;
        }

        private static TraceSource CreateTraceSource(string logFile, string traceName, SourceLevels sourceLevel)
        {
            TraceSource traceSource;
            traceSource = new TraceSource(traceName);
            //ConsoleTraceListener cl = new ConsoleTraceListener();
            //traceSource.Listeners.Add(cl);
            fs = new FileStream(logFile, FileMode.Append, FileAccess.Write,FileShare.ReadWrite); 
            isFsOpen = true;

            traceSource.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(fs));
            traceSource.Switch.Level = sourceLevel;

            traceSource.TraceData(TraceEventType.Verbose, 0, "=================================================");
            traceSource.TraceData(TraceEventType.Verbose, 0, "** Start new log at: " + DateTime.Now.ToString());
            traceSource.TraceData(TraceEventType.Verbose, 0, "=================================================");

            return traceSource;
        }

//        public static void CopyLogsToServer(string serverLogDir)
//        {
//
//            StringBuilder sb = new StringBuilder(20480); 
//            Log("Shutting down logging, copy logs to server...", TraceEventType.Information);
//            Directory.CreateDirectory(serverLogDir);
//            
//            Logger.Close();
//            bool copyErrors = false;    
//            string errorLogName = Path.Combine(Environment.CurrentDirectory,"CopyError.txt");
//            string[] logs = DirMethods.GetFilesRetry(Environment.CurrentDirectory, "FunParser*.log");
//            foreach (string log in logs)
//            {
//                try
//                {      
//                    sb.AppendFormat($"Copying {log}\n");
//                    string toFile = Path.Combine(serverLogDir, Path.GetFileNameWithoutExtension(log) + ".LogSaved");
//                    if( File.Exists(toFile) )
//                    {
//                        File.Delete(toFile);
//                    }     
//                    File.Copy(log, toFile, true);
//                    string savedLog = $"{Path.GetFileNameWithoutExtension(log)}.LogSaved";
//                    File.Copy(log, savedLog,true);
//  					File.Delete(log);
//                }
//                catch(Exception ex) 
//                {
//                    copyErrors = true;
//                    Console.ForegroundColor=ConsoleColor.White;
//                    Console.BackgroundColor=ConsoleColor.DarkRed;
//                    string errorString = $"Error copying log [{log}] to server... Error:\n";
//                    Console.Write(errorString);
//                    sb.AppendFormat(errorString);
//                    string  copyErrorInfo = Utils.WalkExceptionChain(ex);
//                    Console.WriteLine($"\n{copyErrorInfo}");
//                    sb.Append(copyErrorInfo);
//                    Console.ResetColor();
//                }
//            }
//
//            if( copyErrors )
//            {
//                File.AppendAllText(errorLogName,sb.ToString());
//                Console.ForegroundColor=ConsoleColor.White;
//                Console.BackgroundColor=ConsoleColor.DarkRed;
//                Console.WriteLine($"Error were encountred...");
//                Console.WriteLine($"Please send the file \"{errorLogName}\" to te parser team for reveiw." );
//                Console.ForegroundColor=ConsoleColor.White;
//                Console.BackgroundColor=ConsoleColor.DarkBlue;
//                Console.WriteLine($"\n\nThanks....");
//                Console.ResetColor();
//            }
//        }
    }

    public class Utils
    {
    
        public static string WalkExceptionChain( Exception ex ) 
        {
            StringBuilder sb = new StringBuilder(2048);
            sb.Append($"\n{ex.Message}");


            if ( ex.InnerException != null ) 
            {
                sb.Append($"\n{WalkExceptionChain(ex.InnerException)}");
            }
            return sb.ToString();
        }

        public static void SaveObjectToFileJson<T>(T objectsToSave, string fileName)
        {
            using (StreamWriter file = File.CreateText(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, objectsToSave);
            }
        }

        public static T ReadObjectFromFileJson<T>(string fileName)
        {
            using (StreamReader file = File.OpenText(fileName))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (T) serializer.Deserialize(file, typeof(T));
            }
        }

    }
}
