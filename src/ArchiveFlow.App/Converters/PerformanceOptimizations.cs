using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Data;

namespace ArchiveFlow.App.Converters;

/// <summary>
/// Optimized converter for checking equality (used in parameter visibility)
/// </summary>
public class IsEqualConverter : IValueConverter
{
    public static IsEqualConverter Instance { get; } = new IsEqualConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
        {
            return str.Equals(param, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts file size to human-readable format (optimized)
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public static FileSizeConverter Instance { get; } = new FileSizeConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
            return $"{bytes / 1073741824.0:F1} GB";
        }
        return "0 B";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Null to boolean converter (optimized)
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public static NullToBoolConverter Instance { get; } = new NullToBoolConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts node library category state to a compact geometry icon.
/// </summary>
public class NodeTypeToIconConverter : IValueConverter
{
    public static NodeTypeToIconConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool isCategory && isCategory
            ? "M3 4h6l2 2h10v14H3V4z"
            : "M12 2 2 7l10 5 10-5-10-5zm0 12L2 9v8l10 5 10-5V9l-10 5z";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a file extension to a simple geometry icon.
/// </summary>
public class ExtensionToIconConverter : IValueConverter
{
    public static ExtensionToIconConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var extension = value?.ToString()?.ToLowerInvariant() ?? string.Empty;

        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "M4 5h16v14H4V5zm2 2v10h12V7H6zm2 8 3-4 2 3 1-1 3 4H8z",
            ".txt" or ".md" or ".csv" or ".json" or ".xml" or ".cs" or ".py" or ".js" => "M6 2h8l4 4v16H6V2zm7 1.5V7h3.5L13 3.5zM8 11h8v1.5H8V11zm0 4h8v1.5H8V15z",
            ".mp4" or ".wav" or ".mp3" => "M4 6h10v12H4V6zm12 4 5-3v10l-5-3v-4z",
            _ => "M6 2h8l4 4v16H6V2zm7 1.5V7h3.5L13 3.5z"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Object to boolean converter for IsNotNull check
/// </summary>
public class ObjectConverters
{
    public static readonly IValueConverter IsNotNull = new FuncValueConverter<object, bool>(x => x != null);
}

/// <summary>
/// Generic function-based value converter
/// </summary>
public class FuncValueConverter<TFrom, TTo> : IValueConverter
{
    private readonly Func<TFrom, TTo> _func;

    public FuncValueConverter(Func<TFrom, TTo> func)
    {
        _func = func;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TFrom from)
            return _func(from);
        return default(TTo)!;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
