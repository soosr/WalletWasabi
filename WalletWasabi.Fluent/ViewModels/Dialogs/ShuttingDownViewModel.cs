using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using WalletWasabi.Fluent.ViewModels.Navigation;

namespace WalletWasabi.Fluent.ViewModels.Dialogs;

[NavigationMetaData(Title = "Please wait to shut down...")]
public partial class ShuttingDownViewModel : RoutableViewModel
{
	private readonly ApplicationViewModel _applicationViewModel;

	public ShuttingDownViewModel(ApplicationViewModel applicationViewModel)
	{
		_applicationViewModel = applicationViewModel;
		NextCommand = CancelCommand;
	}

	protected override async Task OnNavigatedToAsync(bool isInHistory, CompositeDisposable disposables)
	{
		Observable.Interval(TimeSpan.FromSeconds(3))
				  .ObserveOn(RxApp.MainThreadScheduler)
				  .Subscribe(_ =>
				  {
					  if (_applicationViewModel.CanShutdown())
					  {
						  Navigate().ClearAsync();
						  _applicationViewModel.ShutDown();
					  }
				  })
				  .DisposeWith(disposables);
	}
}
