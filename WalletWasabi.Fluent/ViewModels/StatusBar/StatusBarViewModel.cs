using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using NBitcoin.Protocol;
using ReactiveUI;
using WalletWasabi.BitcoinP2p;
using WalletWasabi.Models;

namespace WalletWasabi.Fluent.ViewModels.StatusBar
{
	public partial class StatusBarViewModel
	{
		[AutoNotify] private TorStatus _torStatus;
		[AutoNotify] private BackendStatus _backendStatus;
		[AutoNotify] private int _peers;

		public StatusBarViewModel()
		{
			UseTor = Services.Config.UseTor;
			TorStatus = UseTor ? Services.Synchronizer.TorStatus : TorStatus.TurnedOff;
		}

		private CompositeDisposable Disposables { get; } = new ();

		private bool UseTor { get; }

		public void Initialize()
		{
			var nodes = Services.HostedServices.Get<P2pNetwork>().Nodes.ConnectedNodes;
			var synchronizer = Services.Synchronizer;

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
		}
	}
}
