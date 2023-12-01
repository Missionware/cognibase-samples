using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PingerAvaloniaApp
{
    public class StatusValueConverter : IValueConverter
    {
        public static StatusValueConverter Instance = new();
        private readonly Bitmap _failBmp;

        private readonly Bitmap _successBmp;
        private readonly Bitmap _unknownBmp;

        public StatusValueConverter()
        {
            string assemblyName = GetType().Assembly.GetName().Name;
            _successBmp = new Bitmap(AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/check-circle2.png")));
            _failBmp = new Bitmap(AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/warning-triangle.png")));
            _unknownBmp = new Bitmap(AssetLoader.Open(new Uri($"avares://{assemblyName}/Assets/info-empty.png")));
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is int && targetType.IsAssignableFrom(typeof(Bitmap)))
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