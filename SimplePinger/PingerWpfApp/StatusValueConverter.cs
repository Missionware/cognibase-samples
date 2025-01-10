using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;


namespace PingerWpfApp
{
    public class StatusValueConverter : IValueConverter
    {
        public static StatusValueConverter Instance = new();
        private readonly BitmapImage _failBmp;

        private readonly BitmapImage _successBmp;
        private readonly BitmapImage _unknownBmp;

        public StatusValueConverter()
        {
            _successBmp = new BitmapImage(new Uri("pack://application:,,,/images/check-circle2.png")); 
            _failBmp = new BitmapImage(new Uri("pack://application:,,,/images/warning-triangle.png"));
            _unknownBmp = new BitmapImage(new Uri("pack://application:,,,/images/info-empty.png"));
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is int && targetType.IsAssignableFrom(typeof(BitmapImage)))
            {
                int status = (int)value;
                if (status == 0)
                    return _unknownBmp;
                if (status == 1)
                    return _failBmp;
                return _successBmp;
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}