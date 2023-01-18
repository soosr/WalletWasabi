using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Logging;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WalletWasabi.Fluent.ViewModels.Settings;

[NavigationMetaData(
	Title = "General",
	Caption = "Manage general settings",
	Order = 0,
	Category = "Settings",
	Keywords = new[]
	{
			"Settings", "General", "Bitcoin", "Dark", "Mode", "Run", "Wasabi", "Computer", "System", "Start", "Background", "Close",
			"Auto", "Copy", "Paste", "Addresses", "Custom", "Change", "Address", "Fee", "Display", "Format", "BTC", "sats"
	},
	IconName = "settings_general_regular")]
public partial class GeneralSettingsTabViewModel : SettingsTabViewModelBase
{
	[ObservableProperty] private bool _darkModeEnabled;
	[ObservableProperty] private bool _autoCopy;
	[ObservableProperty] private bool _autoPaste;
	[ObservableProperty] private bool _customChangeAddress;
	[ObservableProperty] private FeeDisplayUnit _selectedFeeDisplayUnit;
	[ObservableProperty] private bool _runOnSystemStartup;
	[ObservableProperty] private bool _hideOnClose;
	[ObservableProperty] private bool _useTor;
	[ObservableProperty] private bool _terminateTorOnExit;
	[ObservableProperty] private bool _downloadNewVersion;

	public GeneralSettingsTabViewModel()
	{
		_darkModeEnabled = Services.UiConfig.DarkModeEnabled;
		_autoCopy = Services.UiConfig.Autocopy;
		_autoPaste = Services.UiConfig.AutoPaste;
		_customChangeAddress = Services.UiConfig.IsCustomChangeAddress;
		_runOnSystemStartup = Services.UiConfig.RunOnSystemStartup;
		_hideOnClose = Services.UiConfig.HideOnClose;
		_selectedFeeDisplayUnit = Enum.IsDefined(typeof(FeeDisplayUnit), Services.UiConfig.FeeDisplayUnit)
			? (FeeDisplayUnit)Services.UiConfig.FeeDisplayUnit
			: FeeDisplayUnit.Satoshis;
		_useTor = Services.Config.UseTor;
		_terminateTorOnExit = Services.Config.TerminateTorOnExit;
		_downloadNewVersion = Services.Config.DownloadNewVersion;

		this.WhenAnyValue(x => x.DarkModeEnabled)
			.Skip(1)
			.Subscribe(
				x =>
				{
					Services.UiConfig.DarkModeEnabled = x;
					Navigate(NavigationTarget.CompactDialogScreen).To(new ThemeChangeViewModel(x ? Theme.Dark : Theme.Light));
				});

		this.WhenAnyValue(x => x.AutoCopy)
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Skip(1)
			.Subscribe(x => Services.UiConfig.Autocopy = x);

		this.WhenAnyValue(x => x.AutoPaste)
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Skip(1)
			.Subscribe(x => Services.UiConfig.AutoPaste = x);

		StartupCommand = ReactiveCommand.Create(async () =>
		{
			try
			{
				await StartupHelper.ModifyStartupSettingAsync(RunOnSystemStartup);
				Services.UiConfig.RunOnSystemStartup = RunOnSystemStartup;
			}
			catch (Exception ex)
			{
				Logger.LogError(ex);
				RunOnSystemStartup = !RunOnSystemStartup;
				await ShowErrorAsync(Title, "Couldn't save your change, please see the logs for further information.", "Error occurred.");
			}
		});

		this.WhenAnyValue(x => x.CustomChangeAddress)
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Skip(1)
			.Subscribe(x => Services.UiConfig.IsCustomChangeAddress = x);

		this.WhenAnyValue(x => x.SelectedFeeDisplayUnit)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Skip(1)
			.Subscribe(x => Services.UiConfig.FeeDisplayUnit = (int)x);

		this.WhenAnyValue(x => x.HideOnClose)
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Skip(1)
			.Subscribe(x => Services.UiConfig.HideOnClose = x);

		this.WhenAnyValue(
				x => x.UseTor,
				x => x.TerminateTorOnExit,
				x => x.DownloadNewVersion)
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Throttle(TimeSpan.FromMilliseconds(ThrottleTime))
			.Skip(1)
			.Subscribe(_ => Save());
	}

	public ICommand StartupCommand { get; }

	public IEnumerable<FeeDisplayUnit> FeeDisplayUnits =>
		Enum.GetValues(typeof(FeeDisplayUnit)).Cast<FeeDisplayUnit>();

	protected override void EditConfigOnSave(Config config)
	{
		config.UseTor = UseTor;
		config.TerminateTorOnExit = TerminateTorOnExit;
		config.DownloadNewVersion = DownloadNewVersion;
	}
}
