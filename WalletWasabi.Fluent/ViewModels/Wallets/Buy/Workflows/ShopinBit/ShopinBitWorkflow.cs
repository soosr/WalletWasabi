using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.BuyAnything;
using WalletWasabi.Wallets;
using WalletWasabi.WebClients.ShopWare.Models;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows.ShopinBit;

public sealed partial class ShopinBitWorkflow : Workflow
{
	private readonly BuyAnythingManager _buyAnythingManager;
	private readonly Wallet _wallet;

	public ShopinBitWorkflow(Wallet wallet, Conversation conversation) : base(conversation)
	{
		_wallet = wallet;
		_buyAnythingManager = Services.HostedServices.Get<BuyAnythingManager>();
	}

	public override async Task<Conversation> ExecuteAsync()
	{
		// Initial message + Select Product
		await ExecuteStepAsync(new WelcomeStep(Conversation));

		// Select Country
		await ExecuteStepAsync(new CountryStep(Conversation));

		// Specify your request
		await ExecuteStepAsync(new RequestedItemStep(Conversation));

		// Accept Privacy Policy
		await ExecuteStepAsync(new PrivacyPolicyStep(Conversation));

		// Start Conversation (only if it's a new Conversation)
		await ExecuteStepAsync(new StartConversationStep(Conversation, _wallet));

		// Wait until the Offer is received
		using (WaitForConversationStatus(ConversationStatus.OfferReceived, true))
		{
			// Support Chat (loops until Conversation Updates)
			while (Conversation.ConversationStatus != ConversationStatus.OfferReceived)
			{
				await ExecuteStepAsync(new SupportChatStep(Conversation));
			}
		}

		// Firstname
		await ExecuteStepAsync(new FirstNameStep(Conversation));

		// Lastname
		await ExecuteStepAsync(new LastNameStep(Conversation));

		// Streetname
		await ExecuteStepAsync(new StreetNameStep(Conversation));

		// Housenumber
		await ExecuteStepAsync(new HouseNumberStep(Conversation));

		// ZIP/Postalcode
		await ExecuteStepAsync(new ZipPostalCodeStep(Conversation));

		// City
		await ExecuteStepAsync(new CityStep(Conversation));

		// State
		await ExecuteStepAsync(new StateStep(Conversation));

		// Accept Terms of service
		await ExecuteStepAsync(new ConfirmTosStep(Conversation));

		// TODO: The wording is reviewed until this point.

		return Conversation;
	}

	public override bool IsEditable(ChatMessage chatMessage)
	{
		return
			Conversation.ConversationStatus switch
			{
				ConversationStatus.OfferReceived =>
					chatMessage.StepName is nameof(FirstNameStep)
										 or nameof(LastNameStep)
										 or nameof(StreetNameStep)
										 or nameof(HouseNumberStep)
										 or nameof(ZipPostalCodeStep)
										 or nameof(CityStep)
										 or nameof(StateStep),
				_ => false
			};
	}

	public override IWorkflowStep? GetEditor(ChatMessage chatMessage)
	{
		return chatMessage.StepName switch
		{
			// I could have used reflection (or a Source Generator LOL)
			nameof(FirstNameStep) => new FirstNameStep(Conversation),
			nameof(LastNameStep) => new LastNameStep(Conversation),
			nameof(StreetNameStep) => new StreetNameStep(Conversation),
			nameof(HouseNumberStep) => new HouseNumberStep(Conversation),
			nameof(ZipPostalCodeStep) => new ZipPostalCodeStep(Conversation),
			nameof(CityStep) => new CityStep(Conversation),
			nameof(StateStep) => new StateStep(Conversation),
			_ => null
		};
	}

	/// <summary>
	/// Listen to Conversation Updates from the Server waiting for the specified Status. Upon that, it updates the Conversation, and optionally Ignores the current Chat Support Step.
	/// </summary>
	/// <returns>an IDisposable for the event subscription.</returns>
	/// <exception cref="InvalidOperationException"></exception>
	private IDisposable WaitForConversationStatus(ConversationStatus status, bool ignoreCurrentSupportStep)
	{
		return
			Observable.FromEventPattern<ConversationUpdateEvent>(_buyAnythingManager, nameof(BuyAnythingManager.ConversationUpdated))
					  .Where(x => x.EventArgs.Conversation.Id == Conversation.Id)
					  .Where(x => x.EventArgs.Conversation.ConversationStatus == status)
					  .Do(x =>
					  {
						  SetConversation(x.EventArgs.Conversation);
						  if (ignoreCurrentSupportStep)
						  {
							  IgnoreCurrentSupportChatStep();
						  }
					  })
					  .Subscribe();
	}

	private void IgnoreCurrentSupportChatStep()
	{
		if (CurrentStep is SupportChatStep support)
		{
			support.Ignore();
		}
	}
}

\
