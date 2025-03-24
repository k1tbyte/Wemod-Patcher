using System;
using System.Threading.Tasks;
using System.Windows;
using WeModPatcher.Core;
using WeModPatcher.View.MainWindow;
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
            this.MainWindow.Show();
        }

        public new static void Shutdown()
        {
            Current.Dispatcher.Invoke(() => Current.Shutdown());
        }
    }
}