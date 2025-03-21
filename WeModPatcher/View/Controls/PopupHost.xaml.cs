using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace WeModPatcher.View.Controls
{
    public partial class PopupHost : Grid
    {
        internal Action Closed;

        public static readonly DependencyProperty PopupContentProperty =
            DependencyProperty.Register("PopupContent", typeof(object), typeof(PopupHost), new PropertyMetadata(null));
        
        internal readonly SemaphoreSlim OpenedSemaphore = new SemaphoreSlim(1, 1);
        
        private DoubleAnimation OpeningAnimation;
        private DoubleAnimation ClosingAnimation;
        
        
        public bool IsOpen
        {
            get => this.Visibility == Visibility.Visible;
            set
            {
                if (value)
                {
                    if(OpenedSemaphore.CurrentCount == 0)
                        return;
                    

                    Visibility            = Visibility.Visible;
                    cancel.Focus();
                    PopupPresenter.BeginAnimation(OpacityProperty, OpeningAnimation);
                    OpenedSemaphore.Wait();
                }
                else
                {
                    PopupPresenter.BeginAnimation(OpacityProperty, ClosingAnimation);
                }

            }
        }
        
        public object PopupContent
        {
            get => GetValue(PopupContentProperty);
            set => SetValue(PopupContentProperty, value);
        }
        
        private void HidePopup(object sender, EventArgs e)
        {
            if (OpenedSemaphore.CurrentCount == 1)
                return;

            IsOpen = false;
        }
        
        private void OnClosing(object sender, EventArgs e)
        {
            if (PopupContent == null)
                return;

            Visibility   = Visibility.Collapsed;
            Closed?.Invoke();
            PopupContent = null;
            Closed       = null;
            OpenedSemaphore.Release();
        }
        
        public PopupHost()
        {
            InitializeComponent();

            PreviewKeyDown += (sender, e) =>
            {
                if (e.Key != Key.Escape) 
                    return;
                
                HidePopup(null, null);
                e.Handled = true;
            };
            
            OpeningAnimation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.4)))
            {
                EasingFunction = App.Current.FindResource("BaseAnimationFunction") as IEasingFunction
            };
            OpeningAnimation.Freeze();

            ClosingAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.2)));
            ClosingAnimation.Completed += OnClosing;
            ClosingAnimation.Freeze();

            this.Splash.DataContext         = this;
            this.PopupPresenter.DataContext = this;
        }
    }
}