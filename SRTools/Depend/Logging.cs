using Microsoft.UI.Xaml.Documents;
using Spectre.Console;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace SRTools.Depend
{
    internal class Logging
    {
        private static readonly string LogFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "Logs");
        private static readonly string LogFileName = $"SRTools_Log_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        private static readonly string LogFilePath = Path.Combine(LogFolderPath, LogFileName);

        static Logging()
        {
            Directory.CreateDirectory(LogFolderPath);  // 确保日志文件夹存在
            File.Create(LogFilePath).Close();  // 创建新的日志文件
        }

        public static void Write(string info, int mode = 0, string programName = null)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var stackTrace = new StackTrace();
            var stackFrame = stackTrace.GetFrame(2);  // '2' gets the method that called Write
            var methodBase = stackFrame.GetMethod();
            var methodName = methodBase.Name;
            var memoryAddress = stackFrame.GetNativeOffset();

            string logMessage = $"[{DateTime.Now:F}][{threadId}][{methodBase}][{methodName}][{memoryAddress}]:";
            string markupText;

            switch (mode)
            {
                case 0:
                    markupText = "[bold White][[INFO]][/]";
                    break;
                case 1:
                    markupText = "[bold Yellow][[WARN]][/]";
                    break;
                case 2:
                    markupText = "[bold Red][[ERROR]][/]";
                    break;
                case 3:
                    markupText = $"[bold White][[BOARDCAST]][/][bold Magenta][[{programName}]][/]";
                    break;
                default:
                    markupText = "[bold White][[INFO]][/]";
                    break;
            }

            AnsiConsole.Write(new Markup(markupText));
            Console.WriteLine(info);
            logMessage += info;

            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }

        public static void WriteNotification(string title, string info)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;

            string logMessage = $"[{DateTime.Now:F}][{threadId}]:";
            string markupText = $"[bold Green][[Notification]][/][[{title}]]";

            AnsiConsole.Write(new Markup(markupText));
            if(info.Contains("\n"))
            {
                info = info.Replace("\n", ",");
            }
            Console.WriteLine(info);
            logMessage += info;

            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }

        public static void WriteCustom(string run, string info)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var stackTrace = new StackTrace();
            var stackFrame = stackTrace.GetFrame(2);  // '2' gets the method that called Write
            var methodBase = stackFrame.GetMethod();
            var methodName = methodBase.Name;
            var memoryAddress = stackFrame.GetNativeOffset();
            string logMessage = $"[{DateTime.Now:F}][{threadId}][{methodBase}][{methodName}][{memoryAddress}]:";

            AnsiConsole.Write(new Markup($"[bold White][[INFO]][/][bold Yellow][[{run}]][/]"));
            Console.WriteLine(info);
            logMessage += info;

            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
    }
}
