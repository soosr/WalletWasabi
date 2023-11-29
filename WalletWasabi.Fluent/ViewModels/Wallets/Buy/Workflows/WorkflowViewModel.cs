using System.Collections.Generic;
using ReactiveUI;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows;

public abstract partial class WorkflowViewModel : ReactiveObject
{
	[AutoNotify] private List<WorkflowStepViewModel>? _steps;
	[AutoNotify(SetterModifier = AccessModifier.Private)] private WorkflowStepViewModel? _currentStep;
	[AutoNotify(SetterModifier = AccessModifier.Private)] private bool _isCompleted;

	private int _nextStepIndex = 0;

	public WorkflowStepViewModel? PeekNextStep()
	{
		if (_steps is null)
		{
			return null;
		}

		if (_nextStepIndex >= _steps.Count || IsCompleted)
		{
			return null;
		}

		return _steps[_nextStepIndex];
	}

	public WorkflowStepViewModel? ExecuteNextStep()
	{
		if (_steps is null)
		{
			CurrentStep = null;
			return null;
		}

		if (_nextStepIndex >= _steps.Count || IsCompleted)
		{
			CurrentStep = null;
			return null;
		}

		var result = true;
		var step = _steps[_nextStepIndex];

		step.UserInputValidator.OnActivation();

		if (step.RequiresUserInput)
		{
			result = step.UserInputValidator.IsValid();
		}

		if (result)
		{
			step.IsCompleted = true;
		}

		if (result)
		{
			if (_nextStepIndex + 1 >= _steps.Count)
			{
				IsCompleted = true;
			}
			else
			{
				_nextStepIndex++;
			}
		}

		CurrentStep = step;
		return step;
	}

	public abstract WorkflowRequest GetResult();
}
