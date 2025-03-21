using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WeModPatcher.Models;

namespace WeModPatcher.View.Popups
{
    public partial class PatchVectorsPopup : UserControl
    {
        private readonly Action<HashSet<EPatchType>> _onApply;

        public PatchVectorsPopup(Action<HashSet<EPatchType>> onApply)
        {
            _onApply = onApply;
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if(ActivateProBox.IsChecked != true && DisableUpdateBox.IsChecked != true && DisableTelemetryBox.IsChecked != true)
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
            
            _onApply(result);
        }
    }
}