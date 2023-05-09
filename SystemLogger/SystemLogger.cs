using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using Spectre.Console;
using System.Runtime.InteropServices;

namespace SystemLogger
{
    public partial class SystemLogger : ServiceBase
    {
        private static SystemLogger CurrentService;
        
        public SystemLogger()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            CurrentService = this;
            
            var timer = new Timer();
            timer.Interval = 300;
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
            
            if(Environment.UserInteractive)
                AnsiConsole.MarkupInterpolated($"[bold white]{DateTime.Now}: Service started in console mode. Press any key to stop");
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new SystemLogger()
                };
                ServiceBase.Run(ServicesToRun);
            }
            
            AllocConsole();
            SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);
            Console.Title = "RAM Load Logger";
        }

        protected override void OnStop()
        {
        }

        private void OnTimer(object sender, ElapsedEventArgs args)
        {
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            float ramLoad = ramCounter.NextValue();

            string logFilePath = @"C:\Temp\ramlog.txt";
            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                string logMessage = DateTime.Now.ToString() + " RAM load: " + ramLoad.ToString() + " MB";
                Console.WriteLine(logMessage);
                writer.WriteLine(logMessage);
            }
        }
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        delegate bool HandlerRoutine(CtrlTypes ctrlType);

        enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        
        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    // Stop the service
                    CurrentService.Stop();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ctrlType), ctrlType, null);
            }
            return true;
        }
    }
}
