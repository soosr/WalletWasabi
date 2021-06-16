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
using WalletWasabi.Models;

namespace WalletWasabi.Fluent.ViewModels.StatusBar
{
	public partial class StatusBarViewModel
	{
		[AutoNotify] private TorStatus _torStatus;
		[AutoNotify] private BackendStatus _backendStatus;
		[AutoNotify] private int _peers;
		[AutoNotify] private RpcStatus? _bitcoinCoreStatus;

		public StatusBarViewModel()
		{
			UseTor = Services.Config.UseTor; // Do not make it dynamic, because if you change this config settings only next time will it activate.
			UseBitcoinCore = Services.Config.StartLocalBitcoinCoreOnStartup;
			_torStatus = UseTor ? Services.Synchronizer.TorStatus : TorStatus.TurnedOff;

			UpdateCommand = ReactiveCommand.CreateFromTask(async () => await IoHelpers.OpenBrowserAsync("https://wasabiwallet.io/#download"));
			AskMeLaterCommand = ReactiveCommand.Create(() => { });
		}

		public ICommand UpdateCommand { get; }

		public ICommand AskMeLaterCommand { get; }

		private CompositeDisposable Disposables { get; } = new ();

		private bool UseTor { get; }

		public bool UseBitcoinCore { get; }

		public void Initialize()
		{
			var nodes = Services.HostedServices.Get<P2pNetwork>().Nodes.ConnectedNodes;
			var synchronizer = Services.Synchronizer;
			var rpcMonitor = Services.HostedServices.GetOrDefault<RpcMonitor>();

			BitcoinCoreStatus = rpcMonitor?.RpcStatus ?? RpcStatus.Unresponsive;

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
		}
	}
}
