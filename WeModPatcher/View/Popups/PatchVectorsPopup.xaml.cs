using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WeModPatcher.Models;
using WeModPatcher.View.Controls;

namespace WeModPatcher.View.Popups
{
    public partial class PatchVectorsPopup : UserControl, IDisposable
    {
        private readonly Action<PatchConfig> _onApply;
        private readonly StackPanel _popupTitleContainer;
        private string _originalTitle;
        private readonly TextBlock _titleTextBlock;

        public PatchVectorsPopup(Action<PatchConfig> onApply)
        {
            _onApply = onApply;
            InitializeComponent();
            _popupTitleContainer = MainWindow.MainWindow.Instance.PopupHost.TitleContainer;
            _titleTextBlock = _popupTitleContainer.Children[0] as TextBlock;
        }
        
        private void BackClicked(object sender, RoutedEventArgs e)
        {
            Dispose();
            PatchMethod.Visibility = Visibility.Collapsed;
            PatchVectors.Visibility = Visibility.Visible;
        }
        
        private void OnRuntimeSelected(object sender, RoutedEventArgs e) 
            => RaiseCallback(EPatchProcessMethod.Runtime);

        private void OnStaticSelected(object sender, RoutedEventArgs e) 
            => RaiseCallback(EPatchProcessMethod.Static);

        private void RaiseCallback(EPatchProcessMethod method)
        {
            if (ActivateProBox.IsChecked != true && DisableUpdateBox.IsChecked != true &&
                DisableTelemetryBox.IsChecked != true)
            {
                return;
            }

            var result = new HashSet<EPatchType>();
            if (ActivateProBox.IsChecked == true)
            {
                result.Add(EPatchType.ActivatePro);
            }

            if (DisableUpdateBox.IsChecked == true)
            {
                result.Add(EPatchType.DisableUpdates);
            }

            _onApply(new PatchConfig
            {
                PatchTypes = result,
                PatchMethod = method
            });
        }

        private void NextClicked(object sender, RoutedEventArgs e)
        {
            _popupTitleContainer.Children.Insert(0, FindResource("BackButton") as Button);
            _originalTitle = _titleTextBlock.Text;
            _titleTextBlock.Text = "Patch method";
            PatchMethod.Visibility = Visibility.Visible;
            PatchVectors.Visibility = Visibility.Collapsed;
        }

        public void Dispose()
        {
            if (PatchVectors.Visibility == Visibility.Collapsed)
            {
                _popupTitleContainer.Children.RemoveAt(0);
                _titleTextBlock.Text = _originalTitle;
            }
        }
    }
}