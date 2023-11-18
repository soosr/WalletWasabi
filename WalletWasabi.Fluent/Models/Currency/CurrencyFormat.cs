using System.Globalization;
using System.Text.RegularExpressions;
using ReactiveUI;
using WalletWasabi.Fluent.Extensions;

namespace WalletWasabi.Fluent.Models.Currency;

/// <summary>
/// Represents a specific Currency's Parsing and Formatting rules.
/// </summary>
public partial class CurrencyFormat : ReactiveObject
{
	public static readonly CurrencyFormat Btc = new()
	{
		CurrencyCode = "BTC",
		IsApproximate = false,
		ZeroWatermarkFormat = "0",
		MaxFractionalDigits = 8,
		MaxIntegralDigits = 8,
		Format = FormatBtcWithExactFractionals
	};

	public static readonly CurrencyFormat Usd = new()
	{
		CurrencyCode = "USD",
		IsApproximate = true,
		ZeroWatermarkFormat = "0.00",
		MaxFractionalDigits = 2,
		MaxIntegralDigits = 12,
		Format = FormatFiatWithExactFractionals,
	};

	public string CurrencyCode { get; init; }
	public bool IsApproximate { get; init; }
	public string ZeroWatermarkFormat { get; init; }
	public int? MaxIntegralDigits { get; init; }
	public int? MaxFractionalDigits { get; init; }
	public Func<decimal, string> Format { get; init; }

	public string Watermark => $"{ZeroWatermarkFormat} {CurrencyCode}";

	/// <summary>
	/// Formats BTC values using as many fractional digits as they currently have.
	/// This is to avoid adding trailing zeros when typing values in the CurrencyEntryBox
	/// </summary>
	/// <param name="amount"></param>
	/// <returns></returns>
	public static string FormatBtcWithExactFractionals(decimal amount)
	{
		var fractionalDigits = Math.Min(amount.CountFractionalDigits(), CurrencyFormat.Btc.MaxFractionalDigits ?? 0);
		var fractionalString = "";
		for (var i = 0; i < fractionalDigits; i++)
		{
			fractionalString += "0";
			if (i == 3) // Leave an empty space after 4th character
			{
				fractionalString += " ";
			}
		}
		var fullString = $"{{0:### ### ### ##0.{fractionalString}}}";
		return string.Format(CurrencyLocalization.InvariantNumberFormat, fullString, amount).Trim();
	}

	/// <summary>
	/// Formats fiat values using as many fractional digits as they currently have.
	/// This is to avoid adding trailing zeros when typing values in the CurrencyEntryBox
	/// </summary>
	/// <param name="amount"></param>
	/// <returns></returns>
	public static string FormatFiatWithExactFractionals(decimal amount)
	{
		var fractionalDigits = amount.CountFractionalDigits();
		return amount.FormattedFiat($"N{fractionalDigits}");
	}

	/// <summary>
	/// Parses the text according to the format rules, and validates that it doesn't exceed the MaxIntegralDigits and MaxFractionalDigits, if specified.
	/// </summary>
	/// <param name="preComposedText"></param>
	/// <returns>the decimal value resulting from the parse</returns>
	public decimal? Parse(string preComposedText)
	{
		var parsable = CleanInvalidCharacters().Replace(preComposedText, "");

		// Parse string value to decimal using Invariant Localization
		if (!decimal.TryParse(parsable, NumberStyles.Number, CurrencyLocalization.InvariantNumberFormat, out var value))
		{
			return null;
		}

		// reject negative numbers
		if (value < 0)
		{
			return null;
		}

		// Reject numbers above the Max Integral number of Digits
		if (MaxIntegralDigits is { } maxIntegral && value.CountIntegralDigits() > maxIntegral)
		{
			return null;
		}

		// Reject numbers above the Max Fractional number of Digits
		if (MaxFractionalDigits is { } maxFractional && value.CountFractionalDigits() > maxFractional)
		{
			return null;
		}

		return value;
	}

	/// <summary>
	/// Used to clean any character except digits and decimal separator
	/// </summary>
	/// <returns></returns>
	[GeneratedRegex($"[^0-9{CurrencyLocalization.DecimalSeparator}]")]
	private static partial Regex CleanInvalidCharacters();
}
