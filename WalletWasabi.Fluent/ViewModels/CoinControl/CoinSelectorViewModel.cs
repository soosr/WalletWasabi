using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using DynamicData;
using DynamicData.Binding;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.TreeDataGrid;
using WalletWasabi.Fluent.ViewModels.CoinControl.Core;
using WalletWasabi.Fluent.ViewModels.Wallets;
using WalletWasabi.Fluent.Views.CoinControl.Core.Cells;
using WalletWasabi.Fluent.Views.CoinControl.Core.Headers;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.CoinControl;

public partial class CoinSelectorViewModel : ViewModelBase, IDisposable
{
	private readonly CompositeDisposable _disposables = new();
	private readonly ObservableCollectionExtended<CoinControlItemViewModelBase> _list;
	private readonly Wallet _wallet;
	private readonly SourceList<CoinControlItemViewModelBase> _source;
	private readonly ObservableCollection<CoinCoinControlItemViewModel> _allCoins;

	[AutoNotify] private IReadOnlyCollection<SmartCoin> _selectedCoins = ImmutableList<SmartCoin>.Empty;

	public CoinSelectorViewModel(WalletViewModel walletViewModel, IList<SmartCoin> initialCoinSelection)
	{
		_wallet = walletViewModel.Wallet;
		_list = new ObservableCollectionExtended<CoinControlItemViewModelBase>();
		_source = new SourceList<CoinControlItemViewModelBase>();
		_allCoins = new ObservableCollection<CoinCoinControlItemViewModel>();

		_source
			.Connect()
			.DisposeMany()
			.Sort(SortExpressionComparer<CoinControlItemViewModelBase>.Descending(x => GetLabelPriority(x)))
			.OnItemAdded(x => _allCoins.AddRange(x.Children.Cast<CoinCoinControlItemViewModel>()))
			.OnItemRemoved(x => _allCoins.RemoveMany(x.Children.Cast<CoinCoinControlItemViewModel>()))
			.Bind(_list)
			.Subscribe();

		_allCoins
			.ToObservableChangeSet()
			.AutoRefresh(x => x.IsSelected)
			.ToCollection()
			.Select(list => new ReadOnlyCollection<SmartCoin>(list.Where(item => item.IsSelected == true).Select(x => x.SmartCoin).ToList()))
			.BindTo(this, x => x.SelectedCoins);

		walletViewModel.UiTriggers.TransactionsUpdateTrigger
			.Do(_ => Refresh())
			.Subscribe()
			.DisposeWith(_disposables);

		TreeDataGridSource = new HierarchicalTreeDataGridSource<CoinControlItemViewModelBase>(_list)
		{
			Columns =
			{
				ChildrenColumn(),
				IndicatorsColumn(),
				AmountColumn(),
				AnonymityScoreColumn(),
				PocketColumn()
			}
		};

		TreeDataGridSource.DisposeWith(_disposables);

		Refresh(_source, initialCoinSelection);
		CollapseUnselectedPockets();
	}

	public HierarchicalTreeDataGridSource<CoinControlItemViewModelBase> TreeDataGridSource { get; }

	public void Dispose()
	{
		_disposables.Dispose();
		_source.Dispose();
	}

	private static void Populate(SourceList<CoinControlItemViewModelBase> source, Wallet wallet)
	{
		var newItems = wallet
			.GetPockets()
			.Select(pocket => new PocketCoinControlItemViewModel(pocket));

		source.Edit(x =>
		{
			x.Clear();
			x.AddRange(newItems);
		});
	}

	private void CollapseUnselectedPockets()
	{
		foreach (var pocket in _list.Where(x => x.IsSelected == false))
		{
			pocket.IsExpanded = false;
		}
	}

	private IList<SmartCoin> GetSelectedCoins()
	{
		return _allCoins
			.Where(x => x.IsSelected == true)
			.Select(x => x.SmartCoin)
			.ToList();
	}

	private void SyncSelectedItems(IList<SmartCoin> selectedCoins)
	{
		foreach (var coinItem in _allCoins)
		{
			coinItem.IsSelected = selectedCoins.Any(x => x == coinItem.SmartCoin);
		}
	}

	private void Refresh()
	{
		Refresh(_source, GetSelectedCoins());
	}

	private void Refresh(SourceList<CoinControlItemViewModelBase> source, IList<SmartCoin> selectedItems)
	{
		Populate(source, _wallet);
		SyncSelectedItems(selectedItems);
	}

	private static Comparison<TSource?> SortAscending<TSource, TProperty>(Func<TSource, TProperty> selector)
	{
		return (x, y) => Comparer<TProperty>.Default.Compare(selector(x!), selector(y!));
	}

	private static Comparison<TSource?> SortDescending<TSource, TProperty>(Func<TSource, TProperty> selector)
	{
		return (x, y) => Comparer<TProperty>.Default.Compare(selector(y!), selector(x!));
	}

	private static int GetLabelPriority(CoinControlItemViewModelBase coin)
	{
		if (coin.Labels == CoinPocketHelper.PrivateFundsText)
		{
			return 3;
		}

		if (coin.Labels == CoinPocketHelper.SemiPrivateFundsText)
		{
			return 2;
		}

		return 1;
	}

	private static int GetIndicatorPriority(CoinControlItemViewModelBase x)
	{
		if (x.IsCoinjoining)
		{
			return 1;
		}

		if (x.BannedUntilUtc.HasValue)
		{
			return 2;
		}

		if (!x.IsConfirmed)
		{
			return 3;
		}

		return 0;
	}

	private static IColumn<CoinControlItemViewModelBase> ChildrenColumn()
	{
		return new HierarchicalExpanderColumn<CoinControlItemViewModelBase>(
			SelectionColumn(),
			group => group.Children,
			node => node.Children.Count > 1,
			node => node.IsExpanded);
	}

	private static TemplateColumn<CoinControlItemViewModelBase> SelectionColumn()
	{
		return new TemplateColumn<CoinControlItemViewModelBase>(
			"",
			new FuncDataTemplate<CoinControlItemViewModelBase>(
				(_, _) => new SelectionCellView(),
				true),
			GridLength.Auto);
	}

	private static IColumn<CoinControlItemViewModelBase> AmountColumn()
	{
		return new PlainTextColumn<CoinControlItemViewModelBase>(
			"Amount",
			node => node.Amount.ToFormattedString(),
			GridLength.Auto,
			new ColumnOptions<CoinControlItemViewModelBase>
			{
				CompareAscending = SortAscending<CoinControlItemViewModelBase, Money>(x => x.Amount),
				CompareDescending = SortDescending<CoinControlItemViewModelBase, Money>(x => x.Amount)
			});
	}

	private static IColumn<CoinControlItemViewModelBase> IndicatorsColumn()
	{
		return new TemplateColumn<CoinControlItemViewModelBase>(
			"",
			new FuncDataTemplate<CoinControlItemViewModelBase>((_, _) => new IndicatorsCellView(), true),
			GridLength.Auto,
			new ColumnOptions<CoinControlItemViewModelBase>
			{
				CompareAscending = SortAscending<CoinControlItemViewModelBase, int>(GetIndicatorPriority),
				CompareDescending = SortDescending<CoinControlItemViewModelBase, int>(GetIndicatorPriority)
			});
	}

	private static IColumn<CoinControlItemViewModelBase> AnonymityScoreColumn()
	{
		return new PlainTextColumn<CoinControlItemViewModelBase>(
			new AnonymityScoreHeaderView(),
			node => node.AnonymityScore.ToString(),
			GridLength.Auto,
			new TextColumnOptions<CoinControlItemViewModelBase>
			{
				CompareAscending = SortAscending<CoinControlItemViewModelBase, int?>(b => b.AnonymityScore),
				CompareDescending = SortDescending<CoinControlItemViewModelBase, int?>(b => b.AnonymityScore)
			});
	}

	private static IColumn<CoinControlItemViewModelBase> PocketColumn()
	{
		return new TemplateColumn<CoinControlItemViewModelBase>(
			"Pocket",
			new FuncDataTemplate<CoinControlItemViewModelBase>((_, _) => new LabelsCellView(), true),
			GridLength.Star,
			new ColumnOptions<CoinControlItemViewModelBase>
			{
				CompareAscending = SortAscending<CoinControlItemViewModelBase, int>(GetLabelPriority),
				CompareDescending = SortDescending<CoinControlItemViewModelBase, int>(GetLabelPriority)
			});
	}
}
