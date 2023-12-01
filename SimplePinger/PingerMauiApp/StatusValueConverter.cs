using System.Globalization;

namespace PingerMauiApp
{
    public class StatusValueConverter : IValueConverter
    {
        public static StatusValueConverter Instance = new();
        private readonly ImageSource _failBmp;

        private readonly ImageSource _successBmp;
        private readonly ImageSource _unknownBmp;

        public StatusValueConverter()
        {
            _successBmp = ImageSource.FromFile("check_circle2.png");
            _failBmp = ImageSource.FromFile("warning_triangle.png");
            _unknownBmp = ImageSource.FromFile("info_empty.png");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is int && targetType.IsAssignableFrom(typeof(ImageSource)))
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