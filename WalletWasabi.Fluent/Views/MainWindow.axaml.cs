using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels;

namespace WalletWasabi.Fluent.Views
{
	public class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
			this.AttachDevTools();
		}

		private void Button_OnClick(object? sender, RoutedEventArgs e)
		{
			RxApp.MainThreadScheduler.Schedule(async () =>
			{
				DataContext = null;
				Close();
				await Task.Delay(2000);

				new MainWindow()
				{
					DataContext = MainViewModel.Instance
				}.Show();
			});
		}
	}
}
