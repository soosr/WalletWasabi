using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels;

namespace WalletWasabi.Fluent.Views
{
	public class MainView : UserControl
	{
		public MainView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void Button_OnClick(object? sender, RoutedEventArgs e)
		{
			RxApp.MainThreadScheduler.Schedule(async () =>
			{
				DataContext = null;
				await Task.Delay(2000);
				DataContext = MainViewModel.Instance;
			});
		}
	}
}
