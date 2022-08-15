using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using ReactiveUI;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.Navigation;
using WalletWasabi.Wallets.PasswordFinder;

namespace WalletWasabi.Fluent.ViewModels.Login.PasswordFinder;

[NavigationMetaData(Title = "Password Finder")]
public partial class ContainsSymbolsViewModel : RoutableViewModel
{
	public ContainsSymbolsViewModel(PasswordFinderOptions options)
	{
		Options = options;

		SetupCancel(enableCancel: true, enableCancelOnEscape: true, enableCancelOnPressed: true);

		EnableBack = true;

		YesCommand = ReactiveCommand.CreateFromTask(async () => await SetAnswerAsync(true));
		NoCommand = ReactiveCommand.CreateFromTask(async () => await SetAnswerAsync(false));
	}

	public PasswordFinderOptions Options { get; }

	public ICommand YesCommand { get; }

	public ICommand NoCommand { get; }

	private async Task SetAnswerAsync(bool ans)
	{
		Options.UseSymbols = ans;
		await Navigate().ToAsync(new SearchPasswordViewModel(Options));
	}
}
