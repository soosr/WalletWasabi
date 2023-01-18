using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets.Home.History.HistoryItems;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Advanced;

[NavigationMetaData(
	Title = "Wallet Stats",
	Caption = "Displays wallet stats",
	IconName = "nav_wallet_24_regular",
	Order = 3,
	Category = "Wallet",
	Keywords = new[] { "Wallet", "Stats", },
	NavBarPosition = NavBarPosition.None,
	NavigationTarget = NavigationTarget.DialogScreen)]
public partial class WalletStatsViewModel : RoutableViewModel
{
	private readonly Wallet _wallet;
	private readonly WalletViewModel _walletViewModel;

	[ObservableProperty] private int _coinCount;
	[ObservableProperty] private string _balance = "";
	[ObservableProperty] private string _confirmedBalance = "";
	[ObservableProperty] private string _unconfirmedBalance = "";
	[ObservableProperty] private int _generatedKeyCount;
	[ObservableProperty] private int _generatedCleanKeyCount;
	[ObservableProperty] private int _generatedLockedKeyCount;
	[ObservableProperty] private int _generatedUsedKeyCount;
	[ObservableProperty] private int _totalTransactionCount;
	[ObservableProperty] private int _nonCoinjointransactionCount;
	[ObservableProperty] private int _coinjoinTransactionCount;

	public WalletStatsViewModel(WalletViewModel walletViewModel)
	{
		_wallet = walletViewModel.Wallet;
		_walletViewModel = walletViewModel;

		UpdateProps();

		NextCommand = ReactiveCommand.Create(() => Navigate().Clear());
		SetupCancel(enableCancel: true, enableCancelOnEscape: true, enableCancelOnPressed: true);
	}

	protected override void OnNavigatedTo(bool isInHistory, CompositeDisposable disposables)
	{
		base.OnNavigatedTo(isInHistory, disposables);

		Observable.FromEventPattern(_wallet, nameof(_wallet.WalletRelevantTransactionProcessed))
			.Subscribe(_ => UpdateProps())
			.DisposeWith(disposables);
	}

	private void UpdateProps()
	{
		// Number of coins in the wallet.
		CoinCount = _wallet.Coins.Unspent().Count();

		// Total amount of money in the wallet.
		Balance = $"{_wallet.Coins.TotalAmount().ToFormattedString()}";

		// Total amount of confirmed money in the wallet.
		ConfirmedBalance = $"{_wallet.Coins.Confirmed().TotalAmount().ToFormattedString()}";

		// Total amount of unconfirmed money in the wallet.
		UnconfirmedBalance = $"{_wallet.Coins.Unconfirmed().TotalAmount().ToFormattedString()}";

		GeneratedKeyCount = _wallet.KeyManager.GetKeys().Count();
		GeneratedCleanKeyCount = _wallet.KeyManager.GetKeys(KeyState.Clean).Count();
		GeneratedLockedKeyCount = _wallet.KeyManager.GetKeys(KeyState.Locked).Count();
		GeneratedUsedKeyCount = _wallet.KeyManager.GetKeys(KeyState.Used).Count();

		var singleCoinjoins = _walletViewModel.History.Transactions.OfType<CoinJoinHistoryItemViewModel>().ToList();
		var groupedCoinjoins = _walletViewModel.History.Transactions.OfType<CoinJoinsHistoryItemViewModel>().ToList();
		var nestedCoinjoins = groupedCoinjoins.SelectMany(x => x.Children).ToList();
		var nonCoinjoins = _walletViewModel.History.Transactions.Where(x => !x.IsCoinJoin).ToList();

		TotalTransactionCount = singleCoinjoins.Count + nestedCoinjoins.Count + nonCoinjoins.Count;
		NonCoinjointransactionCount = nonCoinjoins.Count;
		CoinjoinTransactionCount = singleCoinjoins.Count + nestedCoinjoins.Count;
	}
}
