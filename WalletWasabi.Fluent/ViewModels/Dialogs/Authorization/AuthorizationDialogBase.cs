using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Dialogs.Base;

namespace WalletWasabi.Fluent.ViewModels.Dialogs.Authorization;

public abstract partial class AuthorizationDialogBase : DialogViewModelBase<bool>
{
	[ObservableProperty] private bool _hasAuthorizationFailed;

	[ObservableProperty] // TODO SourceGenerator: protected setter
	private string _authorizationFailedMessage = "The Authorization has failed, please try again.";

	protected AuthorizationDialogBase()
	{
		NextCommand = new AsyncRelayCommand(AuthorizeCoreAsync);

		// EnableAutoBusyOn(NextCommand);
	}

	protected abstract Task<bool> AuthorizeAsync();

	private async Task AuthorizeCoreAsync()
	{
		IsBusy = true;

		HasAuthorizationFailed = !await AuthorizeAsync();

		if (!HasAuthorizationFailed)
		{
			Close(DialogResultKind.Normal, true);
		}

		IsBusy = false;
	}
}
