using WalletWasabi.Fluent.ViewModels.Navigation;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Send
{
	[NavigationMetaData(Title = "Advanced")]
	public partial class SendAdvancedOptionsViewModel : RoutableViewModel
	{
		public SendAdvancedOptionsViewModel()
		{
			SetupCancel(enableCancel: true, enableCancelOnEscape: true, enableCancelOnPressed: true);
			EnableBack = false;
		}
	}
}
