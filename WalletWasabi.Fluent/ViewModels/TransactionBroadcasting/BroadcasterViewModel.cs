using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Navigation;

namespace WalletWasabi.Fluent.ViewModels.TransactionBroadcasting;

[NavigationMetaData(
	Title = "Broadcaster",
	Caption = "Broadcast your transactions here",
	IconName = "live_regular",
	Order = 5,
	Category = "General",
	Keywords = new[] { "Transaction Id", "Input", "Output", "Amount", "Network", "Fee", "Count", "BTC", "Signed", "Paste", "Import", "Broadcast", "Transaction", },
	NavBarPosition = NavBarPosition.None,
	NavigationTarget = NavigationTarget.DialogScreen)]
public partial class BroadcasterViewModel : TriggerCommandViewModel
{
	public override ICommand TargetCommand => ReactiveCommand.CreateFromTask(async () =>
	{
		var dialogResult = await MainViewModel.Instance.DialogScreen.NavigateDialogAsync(new LoadTransactionViewModel(Services.Config.Network));

		if (dialogResult.Result is { } transaction)
		{
			await MainViewModel.Instance.DialogScreen.ToAsync(new BroadcastTransactionViewModel(Services.Config.Network, transaction));
		}
	});
}
