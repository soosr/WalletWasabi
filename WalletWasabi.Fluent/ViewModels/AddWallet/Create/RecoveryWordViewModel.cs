using CommunityToolkit.Mvvm.ComponentModel;
using WalletWasabi.Fluent.Validation;
using WalletWasabi.Models;

namespace WalletWasabi.Fluent.ViewModels.AddWallet.Create;

public partial class RecoveryWordViewModel : ViewModelBase
{
	[ObservableProperty] private string? _input;
	[ObservableProperty] private bool _isConfirmed;

	public RecoveryWordViewModel(int index, string word)
	{
		Index = index;
		Word = word;

		this.ValidateProperty(x => x.Input, ValidateWord);
	}

	public int Index { get; }
	public string Word { get; }

	public void Reset()
	{
		Input = "";
		IsConfirmed = false;
	}

	private void ValidateWord(IValidationErrors errors)
	{
		if (string.IsNullOrWhiteSpace(Input))
		{
			return;
		}

		if (Input.Equals(Word, StringComparison.InvariantCultureIgnoreCase))
		{
			IsConfirmed = true;
			return;
		}

		errors.Add(ErrorSeverity.Error, $"The input does not match recovery word {Index}.");
	}

	public override string ToString()
	{
		return $"{Index}. {Word}";
	}
}
