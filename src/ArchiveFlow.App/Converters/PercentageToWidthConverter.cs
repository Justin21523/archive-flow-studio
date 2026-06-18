using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ArchiveFlow.App.Converters;

/// <summary>
/// Converts a percentage value (0-100) to a pixel width for chart bars.
/// Assumes a maximum width of 400px for 100%.
/// </summary>
public class PercentageToWidthConverter : IValueConverter
{
    public static PercentageToWidthConverter Instance { get; } = new PercentageToWidthConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            // Max width of the chart area is roughly 400px
            return Math.Max(40, (percentage / 100.0) * 400); 
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
