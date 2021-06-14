using Avalonia.Data.Converters;
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
	}
}
