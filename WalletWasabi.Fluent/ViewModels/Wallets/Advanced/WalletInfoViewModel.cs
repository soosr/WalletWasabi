using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using WalletWasabi.Extensions;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Userfacing;
using static WalletWasabi.Blockchain.Keys.WpkhOutputDescriptorHelper;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Advanced;

[NavigationMetaData(
	Title = "Wallet Info",
	Caption = "Displays wallet info",
	IconName = "nav_wallet_24_regular",
	Order = 4,
	Category = "Wallet",
	Keywords = new[] { "Wallet", "Info", },
	NavBarPosition = NavBarPosition.None,
	NavigationTarget = NavigationTarget.DialogScreen)]
public partial class WalletInfoViewModel : RoutableViewModel
{
	[ObservableProperty] private bool _showSensitiveData;
	[ObservableProperty] private string _showButtonText = "Show sensitive data";
	[ObservableProperty] private string _lockIconString = "eye_show_regular";

	public WalletInfoViewModel(WalletViewModelBase walletViewModelBase)
	{
		var wallet = walletViewModelBase.Wallet;
		var network = wallet.Network;
		IsHardwareWallet = wallet.KeyManager.IsHardwareWallet;

		SetupCancel(enableCancel: true, enableCancelOnEscape: true, enableCancelOnPressed: true);

		EnableCancel = !wallet.KeyManager.IsWatchOnly;

		NextCommand = ReactiveCommand.Create(() => Navigate().Clear());

		CancelCommand = ReactiveCommand.Create(() =>
		{
			ShowSensitiveData = !ShowSensitiveData;
			ShowButtonText = ShowSensitiveData ? "Hide sensitive data" : "Show sensitive data";
			LockIconString = ShowSensitiveData ? "eye_hide_regular" : "eye_show_regular";
		});

		if (!wallet.KeyManager.IsWatchOnly)
		{
			var secret = PasswordHelper.GetMasterExtKey(wallet.KeyManager, wallet.Kitchen.SaltSoup(), out _);

			ExtendedMasterPrivateKey = secret.GetWif(network).ToWif();
			ExtendedAccountPrivateKey = secret.Derive(wallet.KeyManager.SegwitAccountKeyPath).GetWif(network).ToWif();
			ExtendedMasterZprv = secret.ToZPrv(network);

			// TODO: Should work for every type of wallet, temporarily disabling it.
			WpkhOutputDescriptors = wallet.KeyManager.GetOutputDescriptors(wallet.Kitchen.SaltSoup(), network);
		}

		ExtendedAccountPublicKey = wallet.KeyManager.SegwitExtPubKey.ToString(network);
		AccountKeyPath = $"m/{wallet.KeyManager.SegwitAccountKeyPath}";
		MasterKeyFingerprint = wallet.KeyManager.MasterFingerprint.ToString();
	}

	public string ExtendedAccountPublicKey { get; }

	public string AccountKeyPath { get; }

	public string? MasterKeyFingerprint { get; }

	public string? ExtendedMasterPrivateKey { get; }

	public string? ExtendedAccountPrivateKey { get; }

	public string? ExtendedMasterZprv { get; }

	public WpkhDescriptors? WpkhOutputDescriptors { get; }

	public bool IsHardwareWallet { get; }
}
