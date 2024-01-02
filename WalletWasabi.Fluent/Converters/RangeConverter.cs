using System.Collections;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;

namespace WalletWasabi.Fluent.Converters;

public class RangeConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is IEnumerable list && parameter is string range)
		{
			var parts = range.Split(new[] { ".." }, StringSplitOptions.None);
			if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
			{
				var count = end - start + 1;
				return list.Cast<object>().Skip(start).Take(count).ToList();
			}
		}

		return AvaloniaProperty.UnsetValue;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException(); // Not needed for one-way binding
	}
}
