using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Dialogs;
using WalletWasabi.Fluent.ViewModels.Dialogs.Base;

namespace WalletWasabi.Fluent.ViewModels.Navigation;

public abstract partial class RoutableViewModel : ViewModelBase, INavigatable
{
	[ObservableProperty] private bool _isBusy;
	[ObservableProperty] private bool _enableCancelOnPressed;
	[ObservableProperty] private bool _enableCancelOnEscape;
	[ObservableProperty] private bool _enableBack;
	[ObservableProperty] private bool _enableCancel;

	public abstract string Title { get; protected set; }

	private CompositeDisposable? _currentDisposable;

	public NavigationTarget CurrentTarget { get; internal set; }

	public virtual NavigationTarget DefaultTarget => NavigationTarget.HomeScreen;

	protected RoutableViewModel()
	{
		BackCommand = new RelayCommand(() => Navigate().Back());
		CancelCommand = new RelayCommand(() => Navigate().Clear());
	}

	public virtual string IconName { get; protected set; } = "navigation_regular";
	public virtual string IconNameFocused { get; protected set; } = "navigation_regular";

	public IRelayCommand? NextCommand { get; protected set; }

	public IRelayCommand? SkipCommand { get; protected set; }

	public IRelayCommand BackCommand { get; protected set; }

	public IRelayCommand CancelCommand { get; protected set; }

	private void DoNavigateTo(bool isInHistory)
	{
		if (_currentDisposable is { })
		{
			throw new Exception("Can't navigate to something that has already been navigated to.");
		}

		_currentDisposable = new CompositeDisposable();

		OnNavigatedTo(isInHistory, _currentDisposable);
	}

	protected virtual void OnNavigatedTo(bool isInHistory, CompositeDisposable disposables)
	{
	}

	private void DoNavigateFrom(bool isInHistory)
	{
		OnNavigatedFrom(isInHistory);

		_currentDisposable?.Dispose();
		_currentDisposable = null;
	}

	public INavigationStack<RoutableViewModel> Navigate()
	{
		var currentTarget = CurrentTarget == NavigationTarget.Default ? DefaultTarget : CurrentTarget;

		return Navigate(currentTarget);
	}

	public static INavigationStack<RoutableViewModel> Navigate(NavigationTarget currentTarget)
	{
		return currentTarget switch
		{
			NavigationTarget.HomeScreen => NavigationState.Instance.HomeScreenNavigation,
			NavigationTarget.DialogScreen => NavigationState.Instance.DialogScreenNavigation,
			NavigationTarget.FullScreen => NavigationState.Instance.FullScreenNavigation,
			NavigationTarget.CompactDialogScreen => NavigationState.Instance.CompactDialogScreenNavigation,
			_ => throw new NotSupportedException(),
		};
	}

	public void SetActive()
	{
		if (NavigationState.Instance.HomeScreenNavigation.CurrentPage is { } homeScreen)
		{
			homeScreen.IsActive = false;
		}

		if (NavigationState.Instance.DialogScreenNavigation.CurrentPage is { } dialogScreen)
		{
			dialogScreen.IsActive = false;
		}

		if (NavigationState.Instance.FullScreenNavigation.CurrentPage is { } fullScreen)
		{
			fullScreen.IsActive = false;
		}

		if (NavigationState.Instance.CompactDialogScreenNavigation.CurrentPage is { } compactDialogScreen)
		{
			compactDialogScreen.IsActive = false;
		}

		IsActive = true;
	}

	public void OnNavigatedTo(bool isInHistory)
	{
		DoNavigateTo(isInHistory);
	}

	void INavigatable.OnNavigatedFrom(bool isInHistory)
	{
		DoNavigateFrom(isInHistory);
	}

	protected virtual void OnNavigatedFrom(bool isInHistory)
	{
	}

	// TODO RelayCommand: not possible. We need to set it manually.
	protected void EnableAutoBusyOn(params ICommand[] commands)
	{
		foreach (var command in commands)
		{
			(command as IReactiveCommand)?.IsExecuting
				.ObserveOn(RxApp.MainThreadScheduler)
				.Skip(1)
				.Subscribe(x => IsBusy = x);
		}
	}

	public async Task<DialogResult<TResult>> NavigateDialogAsync<TResult>(DialogViewModelBase<TResult> dialog)
		=> await NavigateDialogAsync(dialog, CurrentTarget);

	public static async Task<DialogResult<TResult>> NavigateDialogAsync<TResult>(DialogViewModelBase<TResult> dialog, NavigationTarget target, NavigationMode navigationMode = NavigationMode.Normal)
	{
		var dialogTask = dialog.GetDialogResultAsync();

		Navigate(target).To(dialog, navigationMode);

		var result = await dialogTask;

		Navigate(target).Back();

		return result;
	}

	protected async Task ShowErrorAsync(string title, string message, string caption, NavigationTarget target = NavigationTarget.Default)
	{
		var dialog = new ShowErrorDialogViewModel(message, title, caption);

		var navigationTarget = target != NavigationTarget.Default
			? target
			: CurrentTarget == NavigationTarget.CompactDialogScreen
				? NavigationTarget.CompactDialogScreen
				: NavigationTarget.DialogScreen;

		await NavigateDialogAsync(dialog, navigationTarget);
	}

	protected void SetupCancel(bool enableCancel, bool enableCancelOnEscape, bool enableCancelOnPressed)
	{
		EnableCancel = enableCancel;
		EnableCancelOnEscape = enableCancelOnEscape;
		EnableCancelOnPressed = enableCancelOnPressed;
	}
}
