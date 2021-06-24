using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using WalletWasabi.Blockchain.Blocks;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Models;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets
{
	public partial class LoadingViewModel : ActivatableViewModel
	{
		private readonly Wallet _wallet;
		private readonly uint _filtersToSyncCount;
		private readonly uint _filtersToProcessCount;

		[AutoNotify] private double _percent;
		[AutoNotify] private string? _statusText;
		[AutoNotify] private bool _isBackendConnected;

		private uint? _startingFilterIndex;
		private Stopwatch? _stopwatch;
		private bool _isLoading;

		public LoadingViewModel(Wallet wallet)
		{
			_wallet = wallet;
			_statusText = "";
			_percent = 0;
			_isBackendConnected = Services.Synchronizer.BackendStatus == BackendStatus.Connected;
			var segwitActivationHeight = SmartHeader.GetStartingHeader(_wallet.Network).Height;
			_filtersToSyncCount = (uint) Services.BitcoinStore.SmartHeaderChain.HashesLeft;

			if (_wallet.LastProcessedFilter?.Header?.Height is { } processedHeight &&
			    Services.BitcoinStore.SmartHeaderChain.TipHeight is { } tipHeight)
			{
				_filtersToProcessCount = tipHeight - segwitActivationHeight - processedHeight;
			}
		}

		private uint TotalCount => _filtersToProcessCount + _filtersToSyncCount;

		private uint RemainingFiltersToSync => (uint) Services.BitcoinStore.SmartHeaderChain.HashesLeft;

		protected override void OnActivated(CompositeDisposable disposables)
		{
			base.OnActivated(disposables);

			_stopwatch ??= Stopwatch.StartNew();

			Observable.Interval(TimeSpan.FromSeconds(1))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ =>
				{
					var segwitActivationHeight = SmartHeader.GetStartingHeader(_wallet.Network).Height;
					var processedCount = _filtersToSyncCount - RemainingFiltersToSync;

					if (_wallet.LastProcessedFilter?.Header?.Height is { } lastProcessedFilterHeight)
					{
						processedCount += _filtersToProcessCount - lastProcessedFilterHeight - segwitActivationHeight;
					}

					UpdateStatus(processedCount, _stopwatch.ElapsedMilliseconds);
				})
				.DisposeWith(disposables);

			if (!_isLoading)
			{
				Services.Synchronizer.WhenAnyValue(x => x.BackendStatus)
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe(status => IsBackendConnected = status == BackendStatus.Connected)
					.DisposeWith(disposables);

				this.RaisePropertyChanged(nameof(IsBackendConnected));

				Observable.FromEventPattern<bool>(Services.Synchronizer, nameof(Services.Synchronizer.ResponseArrivedIsGenSocksServFail))
					.Select(x => x.EventArgs)
					.Where(x => x)
					.Subscribe(async _ => await LoadWalletAsync(syncFilters: false))
					.DisposeWith(disposables);

				this.WhenAnyValue(x => x.IsBackendConnected)
					.Where(x => x)
					.Subscribe(async _ => await LoadWalletAsync(syncFilters: true))
					.DisposeWith(disposables);
			}
		}

		private async Task LoadWalletAsync(bool syncFilters)
		{
			if (_isLoading)
			{
				return;
			}

			_isLoading = true;

			if (syncFilters)
			{
				while (RemainingFiltersToSync > 0)
				{
					await Task.Delay(1000);
				}
			}

			await UiServices.WalletManager.LoadWalletAsync(_wallet);
		}

		private void UpdateStatus(uint processedCount, double elapsedMilliseconds)
		{
			if (TotalCount == 0)
			{
				return;
			}

			var percent = (decimal) processedCount / TotalCount * 100;
			// _startingFilterIndex ??= processedFilters; // Store the filter index we started on. It is needed for better remaining time calculation.
			// var realProcessedFilters = processedFilters - _startingFilterIndex.Value;
			var remainingCount = TotalCount - processedCount;

			var tempPercent = (uint) Math.Round(percent);

			if (tempPercent == 0 || processedCount == 0)
			{
				return;
			}

			Percent = tempPercent;
			var percentText = $"{Percent}% completed";

			var remainingMilliseconds = elapsedMilliseconds / processedCount * remainingCount;
			var userFriendlyTime = TextHelpers.TimeSpanToFriendlyString(TimeSpan.FromMilliseconds(remainingMilliseconds));
			var remainingTimeText = string.IsNullOrEmpty(userFriendlyTime) ? "" : $"- {userFriendlyTime} remaining";

			StatusText = $"{percentText} {remainingTimeText}";
		}
	}
}
