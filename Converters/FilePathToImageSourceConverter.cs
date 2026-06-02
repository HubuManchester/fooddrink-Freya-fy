using System.Globalization;

namespace NutriLens.Converters;

/// <summary>
/// Converts a local file path string to an ImageSource for XAML bindings.
/// Returns null (no image) when the path is empty or the file doesn't exist.
/// </summary>
public class FilePathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType,
                           object? parameter, CultureInfo culture)
    {
        if (value is string path &&
            !string.IsNullOrEmpty(path) &&
            File.Exists(path))
        {
            return ImageSource.FromFile(path);
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType,
                                object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}