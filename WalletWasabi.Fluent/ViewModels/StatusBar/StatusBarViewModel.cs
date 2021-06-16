using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using NBitcoin.Protocol;
using ReactiveUI;
using WalletWasabi.BitcoinCore.Monitoring;
using WalletWasabi.BitcoinP2p;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Helpers;
using WalletWasabi.Models;
using WalletWasabi.Services;

namespace WalletWasabi.Fluent.ViewModels.StatusBar
{
	public partial class StatusBarViewModel
	{
		[AutoNotify] private TorStatus _torStatus;
		[AutoNotify] private BackendStatus _backendStatus;
		[AutoNotify] private int _peers;
		[AutoNotify] private RpcStatus? _bitcoinCoreStatus;
		[AutoNotify] private UpdateStatus? _updateStatus;
		[AutoNotify] private bool _updateAvailable;
		[AutoNotify] private bool _criticalUpdateAvailable;
		[AutoNotify] private StatusBarState _currentState;

		public StatusBarViewModel()
		{
			UseTor = Services.Config.UseTor; // Do not make it dynamic, because if you change this config settings only next time will it activate.
			UseBitcoinCore = Services.Config.StartLocalBitcoinCoreOnStartup;
			_torStatus = UseTor ? Services.Synchronizer.TorStatus : TorStatus.TurnedOff;

			UpdateCommand = ReactiveCommand.CreateFromTask(async () => await IoHelpers.OpenBrowserAsync("https://wasabiwallet.io/#download"));
			AskMeLaterCommand = ReactiveCommand.Create(() => UpdateAvailable = false);

			this.WhenAnyValue(x => x.UpdateStatus)
				.WhereNotNull()
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status =>
				{
					UpdateAvailable = !status.ClientUpToDate;
					CriticalUpdateAvailable = !status.BackendCompatible;

					if (CriticalUpdateAvailable)
					{
						CurrentState = StatusBarState.CriticalUpdateAvailable;
					}
					else if (UpdateAvailable)
					{
						CurrentState = StatusBarState.UpdateAvailable;
					}
				});

			this.WhenAnyValue(x => x.TorStatus, x => x.BackendStatus, x => x.Peers, x => x.BitcoinCoreStatus)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(tup =>
				{
					var (torStatus, backendStatus, peers, coreStatus) = tup;

					// The source of the p2p connection comes from if we use Core for it or the network.
					var p2pConnected = UseBitcoinCore ? coreStatus?.Success is true : peers >= 1;
					var torConnected = !UseTor || torStatus == TorStatus.Running;

					if (torConnected && backendStatus == BackendStatus.Connected && p2pConnected)
					{
						CurrentState = StatusBarState.Ready;
					}
					else
					{
						CurrentState = StatusBarState.Loading;
					}
				});
		}

		public ICommand UpdateCommand { get; }

		public ICommand AskMeLaterCommand { get; }

		private CompositeDisposable Disposables { get; } = new ();

		private bool UseTor { get; }

		public bool UseBitcoinCore { get; }

		public string BitcoinCoreName => Constants.BuiltinBitcoinNodeName;

		public void Initialize()
		{
			var nodes = Services.HostedServices.Get<P2pNetwork>().Nodes.ConnectedNodes;
			var synchronizer = Services.Synchronizer;
			var rpcMonitor = Services.HostedServices.GetOrDefault<RpcMonitor>();
			var updateChecker = Services.HostedServices.Get<UpdateChecker>();

			BitcoinCoreStatus = rpcMonitor?.RpcStatus ?? RpcStatus.Unresponsive;
			UpdateStatus = updateChecker.UpdateStatus;

			synchronizer.WhenAnyValue(x => x.TorStatus)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status => TorStatus = UseTor ? status : TorStatus.TurnedOff)
				.DisposeWith(Disposables);

			synchronizer.WhenAnyValue(x => x.BackendStatus)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status => BackendStatus = status)
				.DisposeWith(Disposables);

			Peers = TorStatus == TorStatus.NotRunning ? 0 : nodes.Count;
			Observable
				.Merge(Observable.FromEventPattern(nodes, nameof(nodes.Added)).Select(_ => Unit.Default)
				.Merge(Observable.FromEventPattern<NodeEventArgs>(nodes, nameof(nodes.Removed)).Select(_ => Unit.Default)
				.Merge(Services.Synchronizer.WhenAnyValue(x => x.TorStatus).Select(_ => Unit.Default))))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ => Peers = synchronizer.TorStatus == TorStatus.NotRunning ? 0 : nodes.Count) // Set peers to 0 if Tor is not running, because we get Tor status from backend answer so it seems to the user that peers are connected over clearnet, while they are not.
				.DisposeWith(Disposables);

			if (rpcMonitor is { }) // TODO: Is it possible?
			{
				Observable.FromEventPattern<RpcStatus>(rpcMonitor, nameof(rpcMonitor.RpcStatusChanged))
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe(e => BitcoinCoreStatus = e.EventArgs)
					.DisposeWith(Disposables);
			}

			Observable.FromEventPattern<UpdateStatus>(updateChecker, nameof(updateChecker.UpdateStatusChanged))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(e => UpdateStatus = e.EventArgs)
				.DisposeWith(Disposables);
		}
	}
}
