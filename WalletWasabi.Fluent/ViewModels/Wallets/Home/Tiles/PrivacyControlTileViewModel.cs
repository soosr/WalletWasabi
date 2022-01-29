using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows.Input;
using Avalonia.Threading;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.Models;
using WalletWasabi.WabiSabi.Client;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Home.Tiles;

public partial class PrivacyControlTileViewModel : TileViewModel
{
	private readonly IObservable<Unit> _balanceChanged;
	private readonly Wallet _wallet;
	private readonly DispatcherTimer _animationTimer;
	[AutoNotify] private bool _isAutoCoinJoinEnabled;
	[AutoNotify] private bool _isBoosting;
	[AutoNotify] private bool _showBoostingAnimation;
	[AutoNotify] private bool _boostButtonVisible;
	[AutoNotify] private bool _fullyMixed;
	[AutoNotify] private IList<(string color, double percentShare)>? _testDataPoints;
	[AutoNotify] private IList<DataLegend>? _testDataPointsLegend;
	[AutoNotify] private string _percentText;
	[AutoNotify] private decimal[] _outputsData;
	[AutoNotify] private decimal[] _inputsData;


	public PrivacyControlTileViewModel(WalletViewModel walletVm, IObservable<Unit> balanceChanged)
	{
		_wallet = walletVm.Wallet;
		_balanceChanged = balanceChanged;
		_percentText = "";

		_animationTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(30)
		};

		_animationTimer.Tick += (_, _) =>
		{
			ShowBoostingAnimation = !ShowBoostingAnimation;

			_animationTimer.Interval = ShowBoostingAnimation ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(30);
		};

		walletVm.Settings.WhenAnyValue(x => x.AutoCoinJoin).Subscribe(x => IsAutoCoinJoinEnabled = x);

		walletVm.WhenAnyValue(x => x.IsCoinJoining)
			.Subscribe(x =>
			{
				if (x)
				{
					StartBoostAnimation();
				}
				else
				{
					StopBoostAnimation();
				}
			});

		this.WhenAnyValue(x => x.IsAutoCoinJoinEnabled, x => x.IsBoosting)
			.Subscribe(x =>
			{
				var (autoCjEnabled, isBoosting) = x;

				BoostButtonVisible = !autoCjEnabled && !isBoosting && CanCoinJoin();

				if (autoCjEnabled && isBoosting)
				{
					IsBoosting = false;
				}
			});

		BoostPrivacyCommand = ReactiveCommand.Create(() =>
		{
			var isBoosting = IsBoosting = _wallet.AllowManualCoinJoin = !IsBoosting;

			if (isBoosting)
			{
				StartBoostAnimation();
			}
			else
			{
				StopBoostAnimation();
			}
		});

		var updater = Services.HostedServices.Get<RoundStateUpdater>();

		updater.CreateRoundAwaiter(state =>
		{
			Console.WriteLine($"State: {state.Phase}");
			Console.WriteLine($"Inputs: {state.CoinjoinState.Inputs.Count}");
			Console.WriteLine($"Outputs: {state.CoinjoinState.Outputs.Count}");

			if (state.CoinjoinState.Inputs.Count > 0)
			{
				InputsData = ScaleValues(state.CoinjoinState.Inputs);
			}

			if (state.CoinjoinState.Outputs.Count > 0)
			{
				OutputsData = ScaleValues(state.CoinjoinState.Outputs);
			}

			return false;
		}, CancellationToken.None);
	}

	public ICommand BoostPrivacyCommand { get; }

	private bool CanCoinJoin()
	{
		var privateThreshold = _wallet.ServiceConfiguration.MinAnonScoreTarget;

		return _wallet.Coins.Any(x => x.HdPubKey.AnonymitySet < privateThreshold);
	}

	private void StartBoostAnimation()
	{
		ShowBoostingAnimation = true;
		_animationTimer.Interval = TimeSpan.FromSeconds(5);
		_animationTimer.IsEnabled = true;
	}

	private void StopBoostAnimation()
	{
		ShowBoostingAnimation = false;
		_animationTimer.IsEnabled = false;
	}

	private static decimal Scale(decimal value , decimal min, decimal max, decimal minScale, decimal maxScale)
	{
		return minScale + (value - min)/(max-min) * (maxScale - minScale);
	}

	private decimal[] ScaleValues(ImmutableList<Coin> coins)
	{
		var logCoins = coins.Select(x => Math.Abs(Math.Log((double)x.Amount.ToDecimal(MoneyUnit.BTC)))).ToArray();

		var yAxisValuesLogScaledMax = logCoins.Max();

		var yAxisScaler = new StraightLineFormula();
		yAxisScaler.CalculateFrom(yAxisValuesLogScaledMax, 0, 0, 1);

		var yAxisValuesScaled = logCoins
			.Select(y => yAxisScaler.GetYforX(y))
			.ToList();

		return yAxisValuesScaled.Select(x=>(decimal)x).ToArray();
	}

	private decimal[] ScaleValues(ImmutableList<TxOut> coins)
	{
		var logCoins = coins.Select(x => Math.Abs(Math.Log((double)x.Value.ToDecimal(MoneyUnit.BTC)))).ToArray();

		var yAxisValuesLogScaledMax = logCoins.Max();

		var yAxisScaler = new StraightLineFormula();
		yAxisScaler.CalculateFrom(yAxisValuesLogScaledMax, 0, 0, 1);

		var yAxisValuesScaled = logCoins
			.Select(y => yAxisScaler.GetYforX(y))
			.ToList();

		return yAxisValuesScaled.Select(x=>(decimal)x).ToArray();
	}

	protected override void OnActivated(CompositeDisposable disposables)
	{
		base.OnActivated(disposables);

		_balanceChanged
			.Subscribe(_ => Update())
			.DisposeWith(disposables);
	}

	private void Update()
	{
		var privateThreshold = _wallet.ServiceConfiguration.MinAnonScoreTarget;

		var privateAmount = _wallet.Coins.FilterBy(x => x.HdPubKey.AnonymitySet >= privateThreshold).TotalAmount();
		var normalAmount = _wallet.Coins.FilterBy(x => x.HdPubKey.AnonymitySet < privateThreshold).TotalAmount();

		var privateDecimalAmount = privateAmount.ToDecimal(MoneyUnit.BTC);
		var normalDecimalAmount = normalAmount.ToDecimal(MoneyUnit.BTC);
		var totalDecimalAmount = privateDecimalAmount + normalDecimalAmount;

		var pcPrivate = totalDecimalAmount == 0M ? 1d : (double)(privateDecimalAmount / totalDecimalAmount);
		var pcNormal = 1 - pcPrivate;

		PercentText = $"{pcPrivate:P}";

		FullyMixed = pcPrivate >= 1d;

		TestDataPoints = new List<(string, double)>
			{
				("#78A827", pcPrivate),
				("#D8DED7", pcNormal)
			};

		TestDataPointsLegend = new List<DataLegend>
			{
				new(privateAmount, "Private", "#78A827", pcPrivate),
				new(normalAmount, "Not Private", "#D8DED7", pcNormal)
			};
	}
}
