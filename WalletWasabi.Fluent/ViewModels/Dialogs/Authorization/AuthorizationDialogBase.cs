using System.Threading.Tasks;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Dialogs.Base;

namespace WalletWasabi.Fluent.ViewModels.Dialogs.Authorization;

public abstract class AuthorizationDialogBase : DialogViewModelBase<bool>
{
	protected AuthorizationDialogBase()
	{
		NextCommand = ReactiveCommand.CreateFromTask(async () =>
		{
			var result = await Authorize();

			if (result)
			{
				Close(DialogResultKind.Normal, result);
			}
		});

		EnableAutoBusyOn(NextCommand);
	}

	protected abstract Task<bool> Authorize();

	protected async Task AuthorizationFailedAsync()
	{
		await ShowErrorAsync("Authorization", "The Authorization has failed, please try again.", "", NavigationTarget.CompactDialogScreen);
	}
}
