using System.Threading.Tasks;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Login.PasswordFinder;

[NavigationMetaData(Title = "Password Finder")]
public partial class PasswordNotFoundViewModel : RoutableViewModel
{
	public PasswordNotFoundViewModel(Wallet wallet)
	{
		NextCommand = ReactiveCommand.Create(() => OnNextAsync(wallet));

		SetupCancel(enableCancel: false, enableCancelOnEscape: true, enableCancelOnPressed: true);
	}

	private async Task OnNextAsync(Wallet wallet)
	{
		var page = new PasswordFinderIntroduceViewModel(wallet);
		await Navigate().ToAsync(page, NavigationMode.Clear);
		if (page.NextCommand is { } cmd)
		{
			cmd.Execute(default);
		}
	}
}
