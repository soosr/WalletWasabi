using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.AddWallet;
using WalletWasabi.Fluent.ViewModels.Login.PasswordFinder;
using WalletWasabi.Userfacing;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets;

public partial class ClosedWalletViewModel : WalletViewModelBase
{
	[AutoNotify] private string _password = "";
	[AutoNotify] private bool _isPasswordNeeded;
	[AutoNotify] private string _errorMessage = "";
	[AutoNotify] private bool _isForgotPasswordVisible;

	protected ClosedWalletViewModel(Wallet wallet) : base(wallet)
	{
		Loading = new LoadingViewModel(wallet);
		IsPasswordNeeded = !wallet.KeyManager.IsWatchOnly;
		WalletType = WalletHelpers.GetType(wallet.KeyManager);

		NextCommand = ReactiveCommand.CreateFromTask(OnNextAsync);
		OkCommand = ReactiveCommand.Create(OnOk);
		ForgotPasswordCommand = ReactiveCommand.Create(() => OnForgotPassword(wallet));

		EnableAutoBusyOn(NextCommand);
	}

	public LoadingViewModel Loading { get; }

	public WalletType WalletType { get; }

	public ICommand OkCommand { get; }

	public ICommand ForgotPasswordCommand { get; }

	private async Task OnNextAsync()
	{
		string? compatibilityPasswordUsed = null;

		var isPasswordCorrect = await Task.Run(() => Wallet.TryLogin(Password, out compatibilityPasswordUsed));

		if (!isPasswordCorrect)
		{
			IsForgotPasswordVisible = true;
			ErrorMessage = "The password is incorrect! Please try again.";
			return;
		}

		if (compatibilityPasswordUsed is { })
		{
			await ShowErrorAsync(Title, PasswordHelper.CompatibilityPasswordWarnMessage, "Compatibility password was used");
		}

		var legalResult = await ShowLegalAsync();

		if (legalResult)
		{
			LoginWallet();
		}
		else
		{
			Wallet.Logout();
			ErrorMessage = "You must accept the Terms and Conditions!";
		}
	}

	private void OnOk()
	{
		Password = "";
		ErrorMessage = "";
	}

	private void OnForgotPassword(Wallet wallet)
	{
		Navigate(NavigationTarget.DialogScreen).To(new PasswordFinderIntroduceViewModel(wallet));
	}

	private void LoginWallet()
	{
		this.RaisePropertyChanged(nameof(IsLoggedIn));
		Loading.Start();
		IsLoading = true;
	}

	private async Task<bool> ShowLegalAsync()
	{
		if (!Services.LegalChecker.TryGetNewLegalDocs(out _))
		{
			return true;
		}

		var legalDocs = new TermsAndConditionsViewModel();

		var dialogResult = await NavigateDialogAsync(legalDocs, NavigationTarget.DialogScreen);

		if (dialogResult.Result)
		{
			await Services.LegalChecker.AgreeAsync();
		}

		return dialogResult.Result;
	}

	public static WalletViewModelBase Create(Wallet wallet)
	{
		return wallet.KeyManager.IsHardwareWallet
			? new ClosedHardwareWalletViewModel(wallet)
			: wallet.KeyManager.IsWatchOnly
				? new ClosedWatchOnlyWalletViewModel(wallet)
				: new ClosedWalletViewModel(wallet);
	}
}
