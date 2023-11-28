namespace WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows.ShopinBit;

public partial class RequestWorkflowInputValidatorViewModel : WorkflowInputValidatorViewModel
{
	public RequestWorkflowInputValidatorViewModel() : base(null)
	{
	}

	public override bool IsValid(string message)
	{
		// TODO: Validate request.
		return !string.IsNullOrWhiteSpace(message);
	}
}
