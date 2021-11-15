using System.Globalization;
using System.Linq;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.TransactionBuilding;

namespace WalletWasabi.Fluent.Helpers
{
	public static class CurrencyUtils
	{
		private static NumberFormatInfo FormatInfo = new()
		{
			CurrencyGroupSeparator = " ",
			NumberGroupSeparator = " ",
			CurrencyDecimalSeparator = ".",
			NumberDecimalSeparator = "."
		};

		public static Money CalculateDestinationAmount(this BuildTransactionResult result, BitcoinAddress address)
		{
			return result.Transaction.Transaction.Outputs.First(x => x.ScriptPubKey == address.ScriptPubKey).Value;
		}

		public static string FormattedBtc(this Money amount)
		{
			return amount.ToDecimal(MoneyUnit.BTC).FormattedBtc();
		}

		public static string FormattedBtc(this decimal amount)
		{
			return string.Format(FormatInfo, "{0:### ### ### ##0.#### ####}", amount).Trim();
		}

		public static string FormattedFiat(this decimal amount)
		{
			return string.Format(FormatInfo, "{0:N2}", amount).Trim();
		}
	}
}
