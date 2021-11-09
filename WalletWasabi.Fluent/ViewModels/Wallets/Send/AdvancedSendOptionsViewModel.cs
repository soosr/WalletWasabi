using System;
using System.Globalization;
using System.Reactive;
using System.Reactive.Linq;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Fluent.Validation;
using WalletWasabi.Fluent.ViewModels.Dialogs.Base;
using WalletWasabi.Models;
using WalletWasabi.Userfacing;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Send
{
	[NavigationMetaData(Title = "Advanced")]
	public partial class AdvancedSendOptionsViewModel : DialogViewModelBase<Unit>
	{
		private readonly TransactionInfo _transactionInfo;
		private readonly Network _network;

		[AutoNotify] private string _customFee;
		[AutoNotify] private string _customChangeAddress;

		public AdvancedSendOptionsViewModel(TransactionInfo transactionInfo, Network network)
		{
			_transactionInfo = transactionInfo;
			_network = network;

			_customFee = transactionInfo.CustomFeeRate != FeeRate.Zero
				? transactionInfo.CustomFeeRate.SatoshiPerByte.ToString(CultureInfo.InvariantCulture)
				: "";

			_customChangeAddress = transactionInfo.CustomChangeAddress is { }
				? transactionInfo.CustomChangeAddress.ToString()
				: "";

			EnableBack = false;
			SetupCancel(enableCancel: true, enableCancelOnEscape: true, enableCancelOnPressed: true);

			this.ValidateProperty(x => x.CustomFee, ValidateCustomFee);
			this.ValidateProperty(x => x.CustomChangeAddress, ValidateCustomChangeAddress);

			var nextCommandCanExecute =
				this.WhenAnyValue(
						x => x.CustomFee,
						x => x.CustomChangeAddress)
					.Select(_ =>
					{
						var noError = !Validations.Any;
						var somethingFilled = CustomFee is not null or "" || CustomChangeAddress is not null or "";

						return noError && somethingFilled;
					});

			NextCommand = ReactiveCommand.Create(OnNext, nextCommandCanExecute);
		}

		private void OnNext()
		{
			_transactionInfo.CustomFeeRate =
				decimal.TryParse(CustomFee, NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"), out var customFee)
					? new FeeRate(customFee)
					: FeeRate.Zero;

			_transactionInfo.CustomChangeAddress = AddressStringParser.TryParse(CustomChangeAddress, _network, out var bitcoinUrlBuilder) ? bitcoinUrlBuilder.Address : null;

			Close(DialogResultKind.Normal, Unit.Default);
		}

		private void ValidateCustomChangeAddress(IValidationErrors errors)
		{
			var address = CustomChangeAddress;

			if (address is null or "")
			{
				return;
			}

			if (!AddressStringParser.TryParse(address, _network, out _))
			{
				errors.Add(ErrorSeverity.Error, "Input a valid BTC address or URL.");
			}
		}

		private void ValidateCustomFee(IValidationErrors errors)
		{
			var customFeeString = CustomFee;

			if (customFeeString is null or "")
			{
				return;
			}

			if (!decimal.TryParse(customFeeString, NumberStyles.AllowDecimalPoint, new CultureInfo("en-US"), out var value))
			{
				errors.Add(ErrorSeverity.Error, "The entered fee is not valid.");
				return;
			}

			if (value == decimal.Zero)
			{
				errors.Add(ErrorSeverity.Error, "Cannot be 0.");
				return;
			}

			try
			{
				_ = new FeeRate(value);
			}
			catch(OverflowException)
			{
				errors.Add(ErrorSeverity.Error, "The entered fee is too high.");
				return;
			}
		}
	}
}
