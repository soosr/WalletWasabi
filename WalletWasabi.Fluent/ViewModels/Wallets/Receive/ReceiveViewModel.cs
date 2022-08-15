using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets.Labels;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Receive;

[NavigationMetaData(
	Title = "Receive",
	Caption = "",
	IconName = "wallet_action_receive",
	NavBarPosition = NavBarPosition.None,
	Searchable = false,
	NavigationTarget = NavigationTarget.DialogScreen)]
public partial class ReceiveViewModel : RoutableViewModel
{
	private readonly Wallet _wallet;
	[AutoNotify] private bool _isExistingAddressesButtonVisible;

	public ReceiveViewModel(Wallet wallet)
	{
		_wallet = wallet;
		SetupCancel(enableCancel: true, enableCancelOnEscape: true, enableCancelOnPressed: true);

		EnableBack = false;

		SuggestionLabels = new SuggestionLabelsViewModel(wallet.KeyManager, Intent.Receive, 3);

		var nextCommandCanExecute =
			SuggestionLabels
				.WhenAnyValue(x => x.Labels.Count).Select(_ => Unit.Default)
				.Merge(SuggestionLabels.WhenAnyValue(x => x.IsCurrentTextValid).Select(_ => Unit.Default))
				.Select(_ => SuggestionLabels.Labels.Count > 0 || SuggestionLabels.IsCurrentTextValid);

		NextCommand = ReactiveCommand.CreateFromTask(OnNextAsync, nextCommandCanExecute);

		ShowExistingAddressesCommand = ReactiveCommand.CreateFromTask(OnShowExistingAddressesAsync);
	}

	public SuggestionLabelsViewModel SuggestionLabels { get; }

	public ICommand ShowExistingAddressesCommand { get; }

	private async Task OnNextAsync()
	{
		var newKey = _wallet.KeyManager.GetNextReceiveKey(new SmartLabel(SuggestionLabels.Labels), out bool minGapLimitIncreased);

		if (minGapLimitIncreased)
		{
			int minGapLimit = _wallet.KeyManager.MinGapLimit;
			int prevMinGapLimit = minGapLimit - 1;
			var minGapLimitMessage = $"Minimum gap limit increased from {prevMinGapLimit} to {minGapLimit}.";

			// TODO: notification
		}

		SuggestionLabels.Labels.Clear();

		await Navigate().ToAsync(new ReceiveAddressViewModel(_wallet, newKey));
	}

	private async Task OnShowExistingAddressesAsync()
	{
		await Navigate().ToAsync(new ReceiveAddressesViewModel(_wallet));
	}

	protected override async Task OnNavigatedToAsync(bool isInHistory, CompositeDisposable disposable)
	{
		await base.OnNavigatedToAsync(isInHistory, disposable);

		IsExistingAddressesButtonVisible = _wallet.KeyManager.GetKeys(x => !x.Label.IsEmpty && !x.IsInternal && x.KeyState == KeyState.Clean).Any();
	}
}
