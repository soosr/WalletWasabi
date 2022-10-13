using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Home.Tiles.PrivacyRing;

public partial class PrivacyBarViewModel : ActivatableViewModel
{
	private readonly IObservable<Unit> _updateTrigger;

	[AutoNotify] private double _width;

	public PrivacyBarViewModel(WalletViewModel walletViewModel, IObservable<Unit> updateTrigger)
	{
		_updateTrigger = updateTrigger;
		Wallet = walletViewModel.Wallet;
	}

	public IObservable<bool> IsEmpty { get; set; } = Observable.Empty<bool>();

	public ObservableCollectionExtended<PrivacyBarItemViewModel> Items { get; } = new();

	public Wallet Wallet { get; }

	protected override void OnActivated(CompositeDisposable disposables)
	{
		base.OnActivated(disposables);

		var source = new SourceList<PrivacyBarItemViewModel>();

		source.DisposeWith(disposables);

		source
			.Connect()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Bind(Items)
			.DisposeMany()
			.Subscribe()
			.DisposeWith(disposables);

		_updateTrigger
			.Select(_ => Wallet.GetPockets())
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(pockets => source.Edit(list => CreateSegments(list, pockets)))
			.DisposeWith(disposables);

		IsEmpty = _updateTrigger
			.Select(_ => !Items.Any())
			.ReplayLastActive();
	}

	private void CreateSegments(IExtendedList<PrivacyBarItemViewModel> list, IEnumerable<Pocket> pockets)
	{
		list.Clear();

		var coinCount = pockets.SelectMany(x => x.Coins).Count();

		if (coinCount == 0d)
		{
			return;
		}

		var result = Enumerable.Empty<PrivacyBarItemViewModel>();

		if (coinCount < UIConstants.PrivacyRingMaxItemCount)
		{
			result = CreateCoinSegments(pockets, coinCount);
		}
		else
		{
			result = CreatePocketSegments(pockets);
		}

		foreach (var item in result)
		{
			list.Add(item);
		}
	}

	private IEnumerable<PrivacyBarItemViewModel> CreateCoinSegments(IEnumerable<Pocket> pockets, int coinCount)
	{
		var total = pockets.Sum(x => Math.Abs(x.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC)));
		var start = 0.0m;

		var usableWidth = Width - (coinCount * 2);

		var usablePockets =
				pockets.Where(x => x.Coins.Any())
					   .OrderByDescending(x => x.Coins.First().HdPubKey.AnonymitySet)
					   .ToList();

		foreach (var pocket in usablePockets)
		{
			var pocketCoins = pocket.Coins.OrderByDescending(x => x.Amount).ToList();

			foreach (var coin in pocketCoins)
			{
				var margin = 2;
				var amount = coin.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC);
				var width = Math.Abs((decimal)usableWidth * amount / total);

				// Artificially enlarge segments smaller than 2 px in order to make them visible.
				if (width < 2)
				{
					width++;
					margin--;
				}

				yield return new PrivacyBarItemViewModel(this, coin, (double)start, (double)width);

				start += width + margin;
			}
		}
	}

	private IEnumerable<PrivacyBarItemViewModel> CreatePocketSegments(IEnumerable<Pocket> pockets)
	{
		var total = pockets.Sum(x => Math.Abs(x.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC)));
		var start = 0.0m;

		var usableWidth = Width - (pockets.Count() * 2);

		var usablePockets =
				pockets.Where(x => x.Coins.Any())
					   .OrderByDescending(x => x.Coins.First().HdPubKey.AnonymitySet)
					   .ToList();

		foreach (var pocket in usablePockets)
		{
			var margin = 2;
			var amount = pocket.Amount.ToDecimal(NBitcoin.MoneyUnit.BTC);

			var width = Math.Abs((decimal)usableWidth * amount / total);

			// Artificially enlarge segments smaller than 2 px in order to make them visible.
			if (width < 2)
			{
				width++;
				margin--;
			}

			yield return new PrivacyBarItemViewModel(pocket, Wallet, (double)start, (double)width);

			start += width + margin;
		}
	}
}
