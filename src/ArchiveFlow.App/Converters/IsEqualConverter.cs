using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ArchiveFlow.App.Converters;

/// <summary>
/// Simple boolean to boolean converter (for inverse visibility etc).
/// </summary>
public class BoolToBoolConverter : IValueConverter
{
    public static BoolToBoolConverter Instance { get; } = new BoolToBoolConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            // If parameter is "inverse", return inverse
            if (parameter is string param && param == "inverse")
                return !b;
            return b;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
