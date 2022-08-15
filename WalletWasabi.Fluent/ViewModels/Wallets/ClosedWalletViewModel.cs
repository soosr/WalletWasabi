using System.Reactive.Disposables;
using System.Threading.Tasks;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Login;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets;

public class ClosedWalletViewModel : WalletViewModelBase
{
	protected ClosedWalletViewModel(Wallet wallet)
		: base(wallet)
	{
		OpenCommand = ReactiveCommand.CreateFromTask(OnOpenAsync);
	}

	public LoadingViewModel? Loading { get; private set; }

	protected override async Task OnNavigatedToAsync(bool isInHistory, CompositeDisposable disposables)
	{
		await base.OnNavigatedToAsync(isInHistory, disposables);

		Loading ??= new LoadingViewModel(Wallet);
		Loading.Activate(disposables);

		IsLoading = true;
	}

	private async Task OnOpenAsync()
	{
		if (!Wallet.IsLoggedIn)
		{
			await Navigate().ToAsync(new LoginViewModel(this), NavigationMode.Clear);
		}
		else
		{
			await Navigate().ToAsync(this, NavigationMode.Clear);
		}
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
