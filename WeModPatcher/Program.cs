using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using WeModPatcher.Core;
using WeModPatcher.Models;
using WeModPatcher.Utils;
using WeModPatcher.View.MainWindow;

namespace WeModPatcher
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            
            List<LogEntry> logEntries = new List<LogEntry>();
            if (args.Length > 0)
            {
                try
                {
                    var patchConfig = JsonConvert.DeserializeObject<PatchConfig>(Extensions.Base64Decode(args[0]));
                    RuntimePatcher.Patch(patchConfig, (message, type) =>
                    {
                        logEntries.Add(new LogEntry
                        {
                            Message = message,
                            LogType = type
                        });
                    });
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    logEntries.Add(new LogEntry
                    {
                        Message = "Runtime patching failed: " + e.Message,
                        LogType = ELogType.Error
                    });
                }

            }

            var application = new App();
            application.InitializeComponent();
            application.MainWindow = new MainWindow();
            foreach (var logEntry in logEntries)
            {
                MainWindow.Instance.ViewModel.LogList.Add(logEntry);
            }
            application.Run();
        }
        
        
        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString());
            Environment.Exit(1);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}