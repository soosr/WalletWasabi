using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Exceptions;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Fluent.ViewModels.Dialogs;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets.Send;
using WalletWasabi.Models;
using WalletWasabi.Wallets;
using WalletWasabi.WebClients.PayJoin;

namespace WalletWasabi.Fluent.Helpers
{
	public static class TransactionHelpers
	{
		public static BuildTransactionResult BuildTransaction(Wallet wallet, BitcoinAddress address, Money amount, SmartLabel labels, FeeRate feeRate, IEnumerable<SmartCoin> coins, bool subtractFee, IPayjoinClient? payJoinClient = null)
		{
			if (payJoinClient is { } && subtractFee)
			{
				throw new InvalidOperationException("Not possible to subtract the fee.");
			}

			var intent = new PaymentIntent(
				destination: address,
				amount: amount,
				subtractFee: subtractFee,
				label: labels);

			var txRes = wallet.BuildTransaction(
				password: wallet.Kitchen.SaltSoup(),
				payments: intent,
				feeStrategy: FeeStrategy.CreateFromFeeRate(feeRate),
				allowUnconfirmed: true,
				allowedInputs: coins.Select(coin => coin.OutPoint),
				payjoinClient: payJoinClient);

			return txRes;
		}

		public static BuildTransactionResult BuildTransaction(Wallet wallet, TransactionInfo transactionInfo, bool subtractFee = false, bool isPayJoin = false)
		{
			if (isPayJoin && subtractFee)
			{
				throw new InvalidOperationException("Not possible to subtract the fee.");
			}

			return BuildTransaction(
				wallet,
				transactionInfo.Address,
				transactionInfo.Amount,
				transactionInfo.Labels,
				transactionInfo.FeeRate,
				transactionInfo.Coins,
				subtractFee,
				isPayJoin ? transactionInfo.PayJoinClient : null);

		}

		public static async Task<SmartTransaction> ParseTransactionAsync(string path, Network network)
		{
			var psbtBytes = await File.ReadAllBytesAsync(path);
			PSBT psbt;

			try
			{
				psbt = PSBT.Load(psbtBytes, network);
			}
			catch
			{
				var text = await File.ReadAllTextAsync(path);
				text = text.Trim();
				try
				{
					psbt = PSBT.Parse(text, network);
				}
				catch
				{
					return new SmartTransaction(Transaction.Parse(text, network), Height.Unknown);
				}
			}

			if (!psbt.IsAllFinalized())
			{
				psbt.Finalize();
			}

			return psbt.ExtractSmartTransaction();
		}

		public static async Task<bool> ExportTransactionToBinaryAsync(BuildTransactionResult transaction)
		{
			var psbtExtension = "psbt";
			var filePath = await FileDialogHelper.ShowSaveFileDialogAsync("Export transaction", psbtExtension);

            if (!string.IsNullOrWhiteSpace(filePath))
            {
            	var ext = Path.GetExtension(filePath);
            	if (string.IsNullOrWhiteSpace(ext))
            	{
	                filePath = $"{filePath}.{psbtExtension}";
            	}
            	await File.WriteAllBytesAsync(filePath, transaction.Psbt.ToBytes());

                return true;
            }

            return false;
		}

		public static async Task BuildTransactionAsNormalAsync(TransactionInfo transactionInfo, Wallet wallet, RoutableViewModel callerViewModel)
		{
			try
			{
				var txRes = await Task.Run(() => BuildTransaction(wallet, transactionInfo));

				if (transactionInfo.UserDidntRequestOptimisation)
				{
					callerViewModel.Navigate().To(new TransactionPreviewViewModel(wallet, transactionInfo, txRes));
				}
				else
				{
					callerViewModel.Navigate().To(new OptimisePrivacyViewModel(wallet, transactionInfo, txRes));
				}

			}
			catch (InsufficientBalanceException)
			{
				var maxAmount = Money.FromUnit(transactionInfo.Coins.Sum(coin => coin.Amount), MoneyUnit.Satoshi);
				var txRes = await Task.Run(() => BuildTransaction(wallet, transactionInfo.Address,
					maxAmount, transactionInfo.Labels, transactionInfo.FeeRate, transactionInfo.Coins, subtractFee: true));
				var dialog = new InsufficientBalanceDialogViewModel(BalanceType.Private, txRes, wallet.Synchronizer.UsdExchangeRate); //TODO: Not always private funds
				var result = await callerViewModel.NavigateDialogAsync(dialog, NavigationTarget.DialogScreen);

				if (result.Result)
				{
					callerViewModel.Navigate().To(new OptimisePrivacyViewModel(wallet, transactionInfo, txRes));
				}
				else
				{
					callerViewModel.Navigate().To(new PrivacyControlViewModel(wallet, transactionInfo));
				}
			}
		}

		public static async Task BuildTransactionAsPayJoinAsync(TransactionInfo transactionInfo, Wallet wallet, RoutableViewModel callerViewModel)
		{
			try
			{
				// Do not add the PayJoin client yet, it will be added before broadcasting.
				var txRes = await Task.Run(() => BuildTransaction(wallet, transactionInfo));
				callerViewModel.Navigate().To(new TransactionPreviewViewModel(wallet, transactionInfo, txRes));
			}
			catch (InsufficientBalanceException)
			{
				await callerViewModel.ShowErrorAsync("Transaction Building",
					"There are not enough private funds to cover the transaction fee", //TODO: not always private funds
					"Wasabi was unable to create your transaction.");
				callerViewModel.Navigate().To(new PrivacyControlViewModel(wallet, transactionInfo));
			}
		}

		public static TimeSpan CalculateConfirmationTime(double targetBlock)
		{
			var timeInMinutes = Math.Ceiling(targetBlock) * 10;
			var time = TimeSpan.FromMinutes(timeInMinutes);
			return time;
		}
	}
}
