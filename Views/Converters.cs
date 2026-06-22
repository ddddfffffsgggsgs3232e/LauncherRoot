using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace LauncherRoot.Views;

public static class BoolConverters
{
    public static readonly InverseBoolConverter Not = new();
    public static readonly BoolToBrushConverter ToBrush = new();
}

public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return value;
    }
}

public class BoolToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.Parse("#4A90D9"));
        return new SolidColorBrush(Color.Parse("#2D2D2D"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}




