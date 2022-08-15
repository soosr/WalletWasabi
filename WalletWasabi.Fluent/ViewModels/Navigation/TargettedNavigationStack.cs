using System.Threading.Tasks;
using WalletWasabi.Fluent.ViewModels.Dialogs.Base;

namespace WalletWasabi.Fluent.ViewModels.Navigation;

public static class NavigationExtensions
{
	public static async Task<DialogResult<T>> NavigateDialogAsync<T>(
		this TargettedNavigationStack stack,
		DialogViewModelBase<T> dialog)
	{
		await stack.ToAsync(dialog);

		var result = await dialog.GetDialogResultAsync();

		await stack.BackAsync();

		return result;
	}
}

public class TargettedNavigationStack : NavigationStack<RoutableViewModel>
{
	private readonly NavigationTarget _target;

	public TargettedNavigationStack(NavigationTarget target)
	{
		_target = target;
	}

	public override async Task ClearAsync()
	{
		if (_target == NavigationTarget.HomeScreen)
		{
			await base.ClearAsync(true);
		}
		else
		{
			await base.ClearAsync();
		}
	}

	protected override void OnPopped(RoutableViewModel page)
	{
		base.OnPopped(page);

		page.CurrentTarget = NavigationTarget.Default;
	}

	protected override void OnNavigated(
		RoutableViewModel? oldPage,
		bool oldInStack,
		RoutableViewModel? newPage,
		bool newInStack)
	{
		base.OnNavigated(oldPage, oldInStack, newPage, newInStack);

		if (oldPage is { } && oldPage != newPage)
		{
			oldPage.IsActive = false;
		}

		if (newPage is { })
		{
			newPage.CurrentTarget = _target;
		}
	}
}
