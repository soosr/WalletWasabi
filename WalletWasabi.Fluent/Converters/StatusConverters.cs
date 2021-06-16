using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WalletWasabi.BitcoinCore.Monitoring;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Models;

namespace WalletWasabi.Fluent.Converters
{
	public static class StatusConverters
	{
		public static readonly IValueConverter TorStatusToString =
			new FuncValueConverter<TorStatus, string>(x => x switch
			{
				TorStatus.Running => "is running",
				TorStatus.NotRunning => "is not running",
				TorStatus.TurnedOff => "is turned off",
				{ } => x.ToString()
			});

		public static readonly IValueConverter BackendStatusToString =
			new FuncValueConverter<BackendStatus, string>(x => x switch
			{
				BackendStatus.Connected => "is connected",
				BackendStatus.NotConnected => "is not connected",
				{ } => x.ToString()
			});

		public static readonly IValueConverter RpcStatusStringConverter =
			new FuncValueConverter<RpcStatus?, string>(status => status is null ? RpcStatus.Unresponsive.ToString() : status.ToString());
	}

	public class StatusBarStateVisibilityConverter : IValueConverter
	{
		public static readonly StatusBarStateVisibilityConverter Instance = new();

		private StatusBarStateVisibilityConverter()
		{
		}

		object? IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (StatusBarState) value == (StatusBarState) parameter;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
