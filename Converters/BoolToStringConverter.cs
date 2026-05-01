using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SmartToolbox.Converters;

public class BoolToStringConverter : IValueConverter
{
    public string TrueValue { get; set; } = "";
    public string FalseValue { get; set; } = "";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? TrueValue : FalseValue;

        // 支持通过 parameter 传入 "trueText|falseText"
        if (parameter is string p)
        {
            var parts = p.Split('|');
            if (parts.Length == 2)
                return b ? parts[0] : parts[1];
        }

        return FalseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
