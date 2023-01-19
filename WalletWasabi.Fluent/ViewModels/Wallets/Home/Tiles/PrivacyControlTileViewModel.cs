using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets.Home.Tiles.PrivacyRing;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Home.Tiles;

public partial class PrivacyControlTileViewModel : ActivatableViewModel, IPrivacyRingPreviewItem
{
	private readonly WalletViewModel _walletVm;
	private readonly Wallet _wallet;
	[ObservableProperty] private bool _fullyMixed;
	[ObservableProperty] private string _percentText = "";
	[ObservableProperty] private string _balancePrivateBtc = "";
	[ObservableProperty] private bool _hasPrivateBalance;
	[ObservableProperty] private bool _showPrivacyBar;

	public PrivacyControlTileViewModel(WalletViewModel walletVm, bool showPrivacyBar = true)
	{
		_wallet = walletVm.Wallet;
		_walletVm = walletVm;
		_showPrivacyBar = showPrivacyBar;

		ShowDetailsCommand = new RelayCommand(ShowDetails, () => !walletVm.IsWalletBalanceZero);

		walletVm.WhenAnyValue(x => x.IsWalletBalanceZero)
				.Subscribe(_ => ShowDetailsCommand.NotifyCanExecuteChanged()); // TODO RelayCommand: canExecute, refactor

		if (showPrivacyBar)
		{
			PrivacyBar = new PrivacyBarViewModel(_walletVm);
		}
	}

	public IRelayCommand ShowDetailsCommand { get; }

	public PrivacyBarViewModel? PrivacyBar { get; }

	protected override void OnActivated(CompositeDisposable disposables)
	{
		base.OnActivated(disposables);

		_walletVm.UiTriggers.PrivacyProgressUpdateTrigger
			.Subscribe(_ => Update())
			.DisposeWith(disposables);

		PrivacyBar?.Activate(disposables);
	}

	private void ShowDetails()
	{
		NavigationState.Instance.DialogScreenNavigation.To(new PrivacyRingViewModel(_walletVm));
	}

	private void Update()
	{
		var privateThreshold = _wallet.AnonScoreTarget;

		var currentPrivacyScore = _wallet.Coins.Sum(x => x.Amount.Satoshi * Math.Min(x.HdPubKey.AnonymitySet - 1, privateThreshold - 1));
		var maxPrivacyScore = _wallet.Coins.TotalAmount().Satoshi * (privateThreshold - 1);
		int pcPrivate = maxPrivacyScore == 0M ? 100 : (int)(currentPrivacyScore * 100 / maxPrivacyScore);

		PercentText = $"{pcPrivate} %";

		FullyMixed = pcPrivate >= 100;

		var privateAmount = _wallet.Coins.FilterBy(x => x.HdPubKey.AnonymitySet >= privateThreshold).TotalAmount();
		HasPrivateBalance = privateAmount > Money.Zero;
		BalancePrivateBtc = $"{privateAmount.ToFormattedString()} BTC";
	}
}
