using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.SearchBar.Patterns;
using WalletWasabi.Fluent.ViewModels.SearchBar.SearchItems;

namespace WalletWasabi.Fluent.ViewModels.SearchBar;

public partial class SearchBarViewModel : ReactiveObject
{
	private readonly ReadOnlyObservableCollection<SearchItemGroup> _groups;
	[AutoNotify] private bool _isSearchListVisible;
	[AutoNotify] private string _searchText = "";

	public SearchBarViewModel(IObservable<IChangeSet<ISearchItem, ComposedKey>> itemsObservable)
	{
		itemsObservable
			.Group(s => s.Category)
			.Transform(group => new SearchItemGroup(group.Key, group.Cache.Connect()))
			.Sort(SortExpressionComparer<SearchItemGroup>.Ascending(x => x.Title))
			.Bind(out _groups)
			.DisposeMany()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe();

		HasResults = itemsObservable
			.Count()
			.Select(i => i > 0)
			.Replay(1)
			.RefCount();

		ActivateFirstItemCommand = ReactiveCommand.Create(() =>
		{
			if (_groups is [{ Items: [IActionableItem item] }])
			{
				item.OnExecution();
				Reset();
			}
		});
	}

	public ReactiveCommand<Unit, Unit> ActivateFirstItemCommand { get; set; }

	public IObservable<bool> HasResults { get; }

	public ReadOnlyObservableCollection<SearchItemGroup> Groups => _groups;

	private void Reset()
	{
		IsSearchListVisible = false;
		SearchText = "";
	}
}
