using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PingerApp
{
    [ValueConversion(typeof(int), typeof(ImageSource))]
    internal class EnumImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            string imgPath = "";
            int pingValue = (int)value;

            if (pingValue == 0)
                imgPath = "/Images/info-empty.png";
            else if (pingValue == 1)
                imgPath = "/Images/check-circle2.png";
            else if (pingValue == 2)
                imgPath = "/Images/warning-triangle.png";

            var bmi = new BitmapImage(new Uri($"pack://application:,,,{imgPath}"));

            return bmi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}