using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Animation;

namespace SpeechPlayground.Animation
{
    public static class ScrollViewerExtensions
    {
        public static readonly DependencyProperty AnimatedVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedVerticalOffset",
                typeof(double),
                typeof(ScrollViewerExtensions),
                new PropertyMetadata(0.0, OnAnimatedVerticalOffsetChanged));

        public static double GetAnimatedVerticalOffset(DependencyObject obj)
        {
            return (double)obj.GetValue(AnimatedVerticalOffsetProperty);
        }

        public static void SetAnimatedVerticalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(AnimatedVerticalOffsetProperty, value);
        }

        private static void OnAnimatedVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
