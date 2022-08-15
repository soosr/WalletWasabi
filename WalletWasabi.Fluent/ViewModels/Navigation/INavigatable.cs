using System.Threading.Tasks;

namespace WalletWasabi.Fluent.ViewModels.Navigation;

public interface INavigatable
{
	Task OnNavigatedToAsync(bool isInHistory);

	void OnNavigatedFrom(bool isInHistory);
}
