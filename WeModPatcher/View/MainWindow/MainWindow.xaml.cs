using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WeModPatcher.View.MainWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Instance;
        public readonly MainWindowVm ViewModel;
        
        public MainWindow()
        {
            InitializeComponent();
            this.ViewModel = new MainWindowVm(this);
            this.DataContext = ViewModel;
            VersionLabel.Text = Constants.Version.ToString();
            Instance = this;

        }
        
        public void OpenPopup(FrameworkElement content, string title = null)
        {
            this.PopupHost.PopupContent = content;
            PopupHost.Title.Text = title;
            PopupHost.IsOpen = true;
        }

        private void OnDragMove(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void OnClosing(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void ClosePopup()
        {
            PopupHost.IsOpen = false;
        }

        private void OpenSourceClicked(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(Constants.RepositoryUrl);
        }
    }
}