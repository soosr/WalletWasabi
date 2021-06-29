using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.CoinJoin.Client.Clients.Queuing;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Fluent.ViewModels.Dialogs.Base;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Logging;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Send
{
	[NavigationMetaData(Title = "Transaction Preview")]
	public partial class TransactionPreviewViewModel : RoutableViewModel
	{
		private readonly Wallet _wallet;
		private readonly TransactionInfo _info;

		[AutoNotify] private string _confirmationTimeText;
		[AutoNotify] private SmartLabel _labels;

		public TransactionPreviewViewModel()
		{
			_labels = new SmartLabel("7, La, Laaaaaaaaaaaaaaaaaaaaaaaabel, Labe, Label, Self, Test");
			SetupCancel(enableCancel: true, enableCancelOnEscape: true, enableCancelOnPressed: false);
			EnableBack = true;
			_confirmationTimeText = "";
		}

		public string AmountText { get; }

		public string AddressText { get; }

		public string FeeText { get; }

		public string? PayJoinUrl { get; }

		public bool IsPayJoin { get; }

		protected override void OnNavigatedTo(bool isInHistory, CompositeDisposable disposables)
		{
			base.OnNavigatedTo(isInHistory, disposables);

			ConfirmationTimeText = $"Approximately {TextHelpers.TimeSpanToFriendlyString(_info.ConfirmationTimeSpan)} ";
			// Labels = _info.Labels;
		}

		private async Task OnNextAsync(BuildTransactionResult transaction)
		{
			var transactionAuthorizationInfo = new TransactionAuthorizationInfo(transaction);

			var authResult = await AuthorizeAsync(transactionAuthorizationInfo);

			if (authResult)
			{
				IsBusy = true;

				try
				{
					var finalTransaction = await GetFinalTransactionAsync(transactionAuthorizationInfo.Transaction, _info);
					await SendTransactionAsync(finalTransaction);
					Navigate().To(new SendSuccessViewModel(_wallet, finalTransaction));
				}
				catch (Exception ex)
				{
					await ShowErrorAsync("Transaction", ex.ToUserFriendlyString(), "Wasabi was unable to send your transaction.");
				}

				IsBusy = false;
			}
		}

		private async Task<bool> AuthorizeAsync(TransactionAuthorizationInfo transactionAuthorizationInfo)
		{
			if (!_wallet.KeyManager.IsHardwareWallet && string.IsNullOrEmpty(_wallet.Kitchen.SaltSoup())) // Do not show auth dialog when password is empty
			{
				return true;
			}

			var authDialog = AuthorizationHelpers.GetAuthorizationDialog(_wallet, transactionAuthorizationInfo);
			var authDialogResult = await NavigateDialogAsync(authDialog, authDialog.DefaultTarget);

			if (!authDialogResult.Result && authDialogResult.Kind == DialogResultKind.Normal)
			{
				await ShowErrorAsync("Authorization", "The Authorization has failed, please try again.", "");
			}

			return authDialogResult.Result;
		}

		private async Task SendTransactionAsync( SmartTransaction transaction)
		{
			// Dequeue any coin-joining coins.
			await _wallet.ChaumianClient.DequeueAllCoinsFromMixAsync(DequeueReason.TransactionBuilding);

			await Services.TransactionBroadcaster.SendTransactionAsync(transaction);
		}

		private async Task<SmartTransaction> GetFinalTransactionAsync(SmartTransaction transaction, TransactionInfo transactionInfo)
		{
			if (transactionInfo.PayJoinClient is { })
			{
				try
				{
					var payJoinTransaction = await Task.Run(() => TransactionHelpers.BuildTransaction(_wallet, transactionInfo, subtractFee: false, isPayJoin: true));
					return payJoinTransaction.Transaction;
				}
				catch (Exception ex)
				{
					Logger.LogError(ex);
				}
			}

			return transaction;
		}
	}
}
