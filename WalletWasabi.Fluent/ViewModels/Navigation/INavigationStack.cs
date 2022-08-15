using System.Threading.Tasks;

namespace WalletWasabi.Fluent.ViewModels.Navigation;

public interface INavigationStack<T> where T : INavigatable
{
	/// <summary>
	/// The Current Page.
	/// </summary>
	T? CurrentPage { get; }

	/// <summary>
	/// True if you can navigate back, else false.
	/// </summary>
	bool CanNavigateBack { get; }

	Task ToAsync(T viewmodel, NavigationMode mode = NavigationMode.Normal);

	Task BackAsync();

	Task BackToAsync(T viewmodel);

	Task BackToAsync<TViewModel>() where TViewModel : T;

	Task ClearAsync();
}
