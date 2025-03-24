using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WeModPatcher.View.Controls
{
    public partial class InfoItem : UserControl
    {
        public static readonly DependencyProperty IconDataProperty = 
            DependencyProperty.Register(nameof(IconData), typeof(Geometry), typeof(InfoItem));
            
        public static readonly DependencyProperty IconColorProperty = 
            DependencyProperty.Register(nameof(IconColor), typeof(Brush), typeof(InfoItem));
            
        public static readonly DependencyProperty TextProperty = 
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(InfoItem));
        
        public Geometry IconData
        {
            get => (Geometry)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }
        
        public Brush IconColor
        {
            get => (Brush)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }
        
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        
        public InfoItem()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}