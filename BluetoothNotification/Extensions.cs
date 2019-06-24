using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BluetoothNotification
{
    public static class Extensions
    {
        public static FrameworkElement Set(this FrameworkElement frameworkElement, int row,int column)
        {
            Grid.SetRow(frameworkElement, row);
            Grid.SetColumn(frameworkElement, column);
            return frameworkElement;
        }
    }
}
