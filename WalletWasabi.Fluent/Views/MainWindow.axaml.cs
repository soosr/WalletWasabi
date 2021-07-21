using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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

		private void WindowBase_OnActivated(object? sender, EventArgs e)
		{
			Console.WriteLine("Active");
		}

		private void WindowBase_OnDeactivated(object? sender, EventArgs e)
		{
			Console.WriteLine("Deactive");
		}
	}
}
