using System;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.Forms.MessageBox;

namespace WeModPatcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            base.OnStartup(e);
        }
        
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            MessageBox.Show(e.ToString());
            Shutdown();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ToString());
            Shutdown();
        }

        public new static void Shutdown()
        {
            Current.Dispatcher.Invoke(() => Current.Shutdown());
        }
    }
}