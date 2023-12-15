using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Aggregation;
using ReactiveUI;
using WalletWasabi.BuyAnything;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.Models.UI;
using WalletWasabi.Fluent.ViewModels.Wallets.Buy.Messages;
using WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows;
using WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows.ShopinBit;
using WalletWasabi.Logging;
using WalletWasabi.WebClients.BuyAnything;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Buy;

public partial class OrderViewModel : ReactiveObject
{
	private readonly ReadOnlyObservableCollection<MessageViewModel> _messages;
	private readonly SourceList<MessageViewModel> _messagesList;
	private readonly UiContext _uiContext;
	private readonly IOrderManager _orderManager;
	private readonly CancellationToken _cancellationToken;
	private readonly BuyAnythingManager _buyAnythingManager;

	[AutoNotify] private string _title;
	[AutoNotify] private bool _isBusy;
	[AutoNotify] private bool _isCompleted;
	[AutoNotify] private bool _hasUnreadMessages;

	public OrderViewModel(UiContext uiContext,
		int id,
		Workflow workflow,
		IOrderManager orderManager,
		CancellationToken cancellationToken)
	{
		Id = id;
		Workflow = workflow;

		_title = workflow.Conversation.MetaData.Title;

		_uiContext = uiContext;

		_orderManager = orderManager;
		_cancellationToken = cancellationToken;

		_buyAnythingManager = Services.HostedServices.Get<BuyAnythingManager>();

		_messagesList = new SourceList<MessageViewModel>();

		_messagesList
			.Connect()
			.Bind(out _messages)
			.Subscribe();

		HasUnreadMessagesObs = _messagesList.Connect()
											.AutoRefresh(x => x.IsUnread)
											.Filter(x => x.IsUnread is true)
											.Count()
											.Select(i => i > 0);

		CanRemoveObs = this.WhenAnyValue(x => x.Workflow.Conversation.Id)
						   .Select(id => id != ConversationId.Empty);

		RemoveOrderCommand = ReactiveCommand.CreateFromTask(RemoveOrderAsync, CanRemoveObs);

		var hasUserMessages =
			_messagesList.CountChanged.Select(_ => _messagesList.Items.Any(x => x is UserMessageViewModel));

		CanResetObs =
			 this.WhenAnyValue(x => x.Workflow.Conversation.Id)
				 .Select(id => id == ConversationId.Empty)
				 .CombineLatest(hasUserMessages, (a, b) => a && b);

		ResetOrderCommand = ReactiveCommand.CreateFromTask(ResetOrderAsync, CanResetObs);

		// TODO: Remove this once we use newer version of DynamicData
		HasUnreadMessagesObs.BindTo(this, x => x.HasUnreadMessages);

		// Update file on disk
		this.WhenAnyValue(x => x.HasUnreadMessages).Where(x => x == false).ToSignal()
			.Merge(_messagesList.Connect().AutoRefresh(x => x.IsPaid).ToSignal())
			.DoAsync(async _ => await UpdateConversationLocallyAsync(GetChatMessages(), _metaData, cancellationToken))
			.Subscribe();
	}

	public Workflow Workflow { get; }

	public IObservable<bool> HasUnreadMessagesObs { get; }

	public IObservable<bool> CanRemoveObs { get; }

	public IObservable<bool> CanResetObs { get; }

	public ReadOnlyObservableCollection<MessageViewModel> Messages => _messages;

	public ICommand RemoveOrderCommand { get; }

	public ICommand ResetOrderCommand { get; }

	public int Id { get; }

	public async Task UpdateOrderAsync(Conversation conversation, CancellationToken cancellationToken)
	{
		if (conversation.Id != Workflow.Conversation.Id)
		{
			return;
		}

		_metaData = conversation.MetaData;
		IsCompleted = conversation.OrderStatus == OrderStatus.Done;
		Title = conversation.MetaData.Title;

		UpdateMessages(conversation.ChatMessages);

		var country = conversation.MetaData.Country;
		if (_statesSource.IsEmpty() && country is { })
		{
			_statesSource = await _buyAnythingManager.GetStatesForCountryAsync(country, cancellationToken);
		}

		var conversationStatusString = conversation.ConversationStatus.ToString();
		if (_conversationStatus != conversationStatusString)
		{
			WorkflowManager.OnInvokeNextWorkflow(conversationStatusString, _statesSource, AddAssistantMessage, cancellationToken);
		}
	}

	private async Task SendAsync(CancellationToken cancellationToken)
	{
		IsBusy = true;

		try
		{
			var result = WorkflowManager.InvokeInputWorkflows(AddUserMessage, AddAssistantMessage, _statesSource, cancellationToken);
			if (!result)
			{
				return;
			}

			if (WorkflowManager.CurrentWorkflow.IsCompleted)
			{
				var chatMessages = GetChatMessages();
				await SendApiRequestAsync(chatMessages, _metaData, cancellationToken);
				await SendChatHistoryAsync(GetChatMessages(), cancellationToken);

				WorkflowManager.OnInvokeNextWorkflow(null, _statesSource, AddAssistantMessage, cancellationToken);
			}
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Error while processing order.");
			Logger.LogError($"Error while processing order: {exception}).");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void AddUserMessage(string message, ChatMessageMetaData metaData)
	{
		var workflowStep = currentWorkflow.CurrentStep;

		UserMessageViewModel? userMessage = null;

		var editMessageAsync = async () =>
		{
			if (userMessage is null)
			{
				return;
			}

			workflowStep.UserInputValidator.Message = userMessage.UiMessage;

			var editedMessage = await _uiContext.Navigate().To().EditMessageDialog(
				workflowStep.UserInputValidator,
				WorkflowManager.WorkflowState).GetResultAsync();

			if (!string.IsNullOrEmpty(editedMessage))
			{
				if (currentWorkflow.TryToEditStep(workflowStep, editedMessage))
				{
					userMessage.UiMessage = editedMessage;
				}
			}
		};

		var editMessageCommand = ReactiveCommand.CreateFromTask(editMessageAsync, currentWorkflow.CanEditObservable);

		userMessage = new UserMessageViewModel(editMessageCommand, canEditObservable, workflowStep, metaData)
		{
			UiMessage = message,
			OriginalText = message
		};

		_messagesList.Edit(x =>
		{
			x.Add(userMessage);
		});
	}

	private async Task RemoveOrderAsync()
	{
		var confirmed = await _uiContext.Navigate().To().ConfirmDeleteOrderDialog(this).GetResultAsync();

		if (confirmed)
		{
			_orderManager.RemoveOrderAsync(Id);
		}
	}

	private async Task ShowErrorAsync(string message)
	{
		await _uiContext.Navigate().To().ShowErrorDialog(message, "Send Failed", "Wasabi was unable to send your message", NavigationTarget.CompactDialogScreen).GetResultAsync();
	}

	private async Task ResetOrderAsync()
	{
		ClearMessages();
		WorkflowManager.ResetWorkflow();
		await StartConversationAsync("Started", null);
	}

	public void UpdateMessages(Chat chat)
	{
		var messages = CreateMessages(chat);

		_messagesList.Edit(x =>
		{
			x.Clear();
			x.Add(messages);
		});
	}

	private void ClearMessages()
	{
		_messagesList.Edit(x =>
		{
			x.Clear();
		});
	}

	private ChatMessage[] GetChatMessages()
	{
		return _messages
			.Select(x =>
			{
				var message = x.OriginalText ?? "";

				return x switch
				{
					PayNowAssistantMessageViewModel payVm => new SystemChatMessage(message, payVm.Invoice, payVm.IsUnread, payVm.MetaData),
					UrlListMessageViewModel urlVm => new SystemChatMessage(message, urlVm.Data, urlVm.IsUnread, urlVm.MetaData),
					OfferMessageViewModel offerVm => new SystemChatMessage(message, offerVm.OfferCarrier, offerVm.IsUnread, offerVm.MetaData),
					AssistantMessageViewModel => new ChatMessage(false, message, x.IsUnread, x.MetaData),
					UserMessageViewModel => new ChatMessage(true, message, x.IsUnread, x.MetaData),
					_ => throw new InvalidOperationException($"Cannot convert {x.GetType()}!")
				};
			})
			.ToArray();
	}

	private Task UpdateConversationLocallyAsync(ChatMessage[] chatMessages, ConversationMetaData metaData, CancellationToken cancellationToken)
	{
		if (WorkflowManager.Id == ConversationId.Empty || WorkflowManager.CurrentWorkflow is null || Services.HostedServices.GetOrDefault<BuyAnythingManager>() is not { } buyAnythingManager)
		{
			return Task.CompletedTask;
		}

		return buyAnythingManager.UpdateConversationOnlyLocallyAsync(WorkflowManager.Id, chatMessages, metaData, cancellationToken);
	}

	private Task SendChatHistoryAsync(ChatMessage[] chatMessages, CancellationToken cancellationToken)
	{
		if (WorkflowManager.Id == ConversationId.Empty || WorkflowManager.CurrentWorkflow is null || Services.HostedServices.GetOrDefault<BuyAnythingManager>() is not { } buyAnythingManager)
		{
			return Task.CompletedTask;
		}

		return buyAnythingManager.UpdateConversationAsync(WorkflowManager.Id, chatMessages, cancellationToken);
	}

	private static List<MessageViewModel> CreateMessages(Chat chat)
	{
		var orderMessages = new List<MessageViewModel>();

		foreach (var message in chat)
		{
			if (message.IsMyMessage)
			{
				var userMessage = new UserMessageViewModel(null, null, null, message.MetaData)
				{
					UiMessage = message.Text,
					OriginalText = message.Text,
					IsUnread = message.IsUnread
				};
				orderMessages.Add(userMessage);
			}
			else
			{
				if (message is SystemChatMessage systemChatMessage)
				{
					switch (systemChatMessage.Data)
					{
						case OfferCarrier offerCarrier:
							orderMessages.Add(new OfferMessageViewModel(offerCarrier, message.MetaData)
							{
								OriginalText = message.Text,
								UiMessage = "I can offer you:",
								IsUnread = message.IsUnread
							});
							continue;
						case Invoice invoice:
							orderMessages.Add(new PayNowAssistantMessageViewModel(invoice, message.MetaData)
							{
								OriginalText = message.Text,
								IsUnread = message.IsUnread
							});
							continue;
						case AttachmentLinks attachmentLinks:
							orderMessages.Add(new UrlListMessageViewModel(attachmentLinks, message.MetaData)
							{
								OriginalText = message.Text,
								UiMessage = "Download your files:",
								IsUnread = message.IsUnread
							});
							continue;
						case TrackingCodes trackingCodes:
							orderMessages.Add(new UrlListMessageViewModel(trackingCodes, message.MetaData)
							{
								OriginalText = message.Text,
								UiMessage = "For shipping updates:",
								IsUnread = message.IsUnread
							});
							continue;
					}
				}

				var userMessage = new AssistantMessageViewModel(null, null, message.MetaData)
				{
					UiMessage = message.Text,
					OriginalText = message.Text,
					IsUnread = message.IsUnread
				};
				orderMessages.Add(userMessage);
			}
		}

		return orderMessages;
	}
}
