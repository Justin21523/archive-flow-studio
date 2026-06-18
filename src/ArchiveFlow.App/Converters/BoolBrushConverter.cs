using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ArchiveFlow.App.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public static BoolToBrushConverter Instance { get; } = new BoolToBrushConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            // Selected color (Blue)
            return new SolidColorBrush(Color.Parse("#007ACC")); 
        }
        
        // Default color (Dark Gray)
        return new SolidColorBrush(Color.Parse("#3F3F46"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsEqualConverter : IValueConverter
{
    public static IsEqualConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
