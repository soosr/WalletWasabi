using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using WalletWasabi.Models;

namespace WalletWasabi.Fluent.ViewModels.StatusBar
{
	public partial class StatusBarViewModel
	{
		[AutoNotify] private TorStatus _torStatus;
		[AutoNotify] private BackendStatus _backendStatus;


		public StatusBarViewModel()
		{
			UseTor = Services.Config.UseTor;

			Services.Synchronizer.WhenAnyValue(x => x.TorStatus)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status => TorStatus = UseTor ? status : TorStatus.TurnedOff)
				.DisposeWith(Disposables);

			Services.Synchronizer.WhenAnyValue(x => x.BackendStatus)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status => BackendStatus = status)
				.DisposeWith(Disposables);
		}

		private CompositeDisposable Disposables { get; } = new CompositeDisposable();

		private bool UseTor { get; }
	}
}
