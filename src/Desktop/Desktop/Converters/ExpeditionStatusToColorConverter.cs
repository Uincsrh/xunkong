﻿using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Xunkong.Desktop.Converters
{
    internal class ExpeditionStatusToColorConverter : IValueConverter
    {

        private static SolidColorBrush green = new SolidColorBrush(Colors.Green);
        private static SolidColorBrush orange = new SolidColorBrush(Colors.Orange);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var status = (bool)value;
            return status switch
            {
                true => green,
                false => orange,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}