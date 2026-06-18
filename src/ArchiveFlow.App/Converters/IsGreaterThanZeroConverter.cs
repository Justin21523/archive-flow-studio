using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ArchiveFlow.App.Converters;

/// <summary>
/// Converts integer count to boolean (true if > 0).
/// </summary>
public class IsGreaterThanZeroConverter : IValueConverter
{
    public static IsGreaterThanZeroConverter Instance { get; } = new IsGreaterThanZeroConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
