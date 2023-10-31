using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.Wallets.Send;
using WalletWasabi.Wallets;

#pragma warning disable CA2000

namespace WalletWasabi.Fluent.Models.Wallets;

[AutoInterface]
public partial class WalletCoinsModel
{
	private readonly Wallet _wallet;
	private readonly IWalletModel _walletModel;

	public WalletCoinsModel(Wallet wallet, IWalletModel walletModel)
	{
		_wallet = wallet;
		_walletModel = walletModel;

		var transactionProcessed = walletModel.Transactions.TransactionProcessed;
		var anonScoreTargetChanged = walletModel.WhenAnyValue(x => x.Settings.AnonScoreTarget).Skip(1).ToSignal();
		var isCoinjoinRunningChanged = walletModel.Coinjoin.IsRunning.ToSignal();

		var signals = transactionProcessed
			.Merge(anonScoreTargetChanged)
			.Merge(isCoinjoinRunningChanged);

		List = signals.Select(_ => GetCoins());
		Pockets = signals.Select(_ => GetPockets());
	}

	public IObservable<ICoinModel[]> List { get; }

	public IObservable<Pocket[]> Pockets { get; }

	public List<ICoinModel> GetSpentCoins(BuildTransactionResult? transaction)
	{
		var coins = (transaction?.SpentCoins ?? new List<SmartCoin>()).ToList();
		return coins.Select(GetCoinModel).ToList();
	}

	public ICoinModel GetCoinModel(SmartCoin smartCoin)
	{
		return new CoinModel(smartCoin, _walletModel.Settings.AnonScoreTarget);
	}

	public bool AreEnoughToCreateTransaction(TransactionInfo transactionInfo, IEnumerable<ICoinModel> coins)
	{
		return TransactionHelpers.TryBuildTransactionWithoutPrevTx(_wallet.KeyManager, transactionInfo, _wallet.Coins, coins.GetSmartCoins(), _wallet.Kitchen.SaltSoup(), out _);
	}

	private Pocket[] GetPockets()
	{
		return _wallet.GetPockets().ToArray();
	}

	private ICoinModel[] GetCoins()
	{
		return _wallet.Coins.Select(GetCoinModel).ToArray();
	}
}
