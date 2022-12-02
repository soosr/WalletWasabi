using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets;

namespace WalletWasabi.Fluent.ViewModels.NavBar;

/// <summary>
/// The ViewModel that represents the structure of the sidebar.
/// </summary>
public partial class NavBarViewModel : ViewModelBase
{
	[AutoNotify(SetterModifier = AccessModifier.Private)]
	private NavBarItemViewModel? _selectedItem;

	public NavBarViewModel()
	{
		TopItems = new ObservableCollection<NavBarItemViewModel>();
		BottomItems = new ObservableCollection<NavBarItemViewModel>();

		Wallets.ToObservableChangeSet().Transform(x => x as NavBarItemViewModel)
			.Merge(TopItems.ToObservableChangeSet())
			.Merge(BottomItems.ToObservableChangeSet())
			.WhenPropertyChanged(x => x.IsSelected)
			.Where(x => x.Value)
			.Select(x => x.Sender)
			.BindTo(this, x => x.SelectedItem);

		this.WhenAnyValue(x => x.SelectedItem)
			.Buffer(2, 1)
			.Select(buffer => (OldValue: buffer[0], NewValue: buffer[1]))
			.Subscribe(x =>
			{
				if (x.OldValue is { } old)
				{
					old.IsSelected = false;
				}

				if (x.NewValue is WalletViewModelBase wallet)
				{
					Services.UiConfig.LastSelectedWallet = wallet.WalletName;
				}
			});
	}

	public ObservableCollection<NavBarItemViewModel> TopItems { get; }

	public ObservableCollection<NavBarItemViewModel> BottomItems { get; }

	public ObservableCollection<WalletViewModelBase> Wallets => UiServices.WalletManager.Wallets;

	public async Task InitialiseAsync()
	{
		var topItems = NavigationManager.MetaData.Where(x => x.NavBarPosition == NavBarPosition.Top);

		var bottomItems = NavigationManager.MetaData.Where(x => x.NavBarPosition == NavBarPosition.Bottom);

		foreach (var item in topItems)
		{
			var viewModel = await NavigationManager.MaterialiseViewModelAsync(item);

			if (viewModel is NavBarItemViewModel navBarItem)
			{
				TopItems.Add(navBarItem);
			}
		}

		foreach (var item in bottomItems)
		{
			var viewModel = await NavigationManager.MaterialiseViewModelAsync(item);

			if (viewModel is NavBarItemViewModel navBarItem)
			{
				BottomItems.Add(navBarItem);
			}
		}
	}
}
