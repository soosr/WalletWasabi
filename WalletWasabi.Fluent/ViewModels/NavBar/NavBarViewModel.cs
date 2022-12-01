using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Fluent.ViewModels.Wallets;

namespace WalletWasabi.Fluent.ViewModels.NavBar;

/// <summary>
/// The ViewModel that represents the structure of the sidebar.
/// </summary>
public partial class NavBarViewModel : ViewModelBase
{
	[AutoNotify] private ObservableCollection<NavBarItemViewModel> _topItems;
	[AutoNotify] private ObservableCollection<NavBarItemViewModel> _bottomItems;

	public NavBarViewModel(TargettedNavigationStack mainScreen)
	{
		_topItems = new ObservableCollection<NavBarItemViewModel>();
		_bottomItems = new ObservableCollection<NavBarItemViewModel>();
	}

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
				_topItems.Add(navBarItem);
			}
		}

		foreach (var item in bottomItems)
		{
			var viewModel = await NavigationManager.MaterialiseViewModelAsync(item);

			if (viewModel is NavBarItemViewModel navBarItem)
			{
				_bottomItems.Add(navBarItem);
			}
		}
	}
}
