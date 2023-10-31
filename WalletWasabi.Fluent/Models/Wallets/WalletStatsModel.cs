using ReactiveUI;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.Models.Wallets;

public partial interface IWalletStatsModel : IDisposable
{
}

[AutoInterface]
public partial class WalletStatsModel : ReactiveObject, IDisposable
{
	private readonly CompositeDisposable _disposables = new();
	[AutoNotify] private int _coinCount;
	[AutoNotify] private Amount _balance;
	[AutoNotify] private Amount _confirmedBalance;
	[AutoNotify] private Amount _unconfirmedBalance;
	[AutoNotify] private int _generatedKeyCount;
	[AutoNotify] private int _generatedCleanKeyCount;
	[AutoNotify] private int _generatedLockedKeyCount;
	[AutoNotify] private int _generatedUsedKeyCount;
	[AutoNotify] private int _totalTransactionCount;
	[AutoNotify] private int _nonCoinjointransactionCount;
	[AutoNotify] private int _coinjoinTransactionCount;

	public WalletStatsModel(IWalletModel walletModel, Wallet wallet)
	{
		_balance = Amount.Zero;
		_confirmedBalance = Amount.Zero;
		_unconfirmedBalance = Amount.Zero;

		walletModel.Transactions.List
			.Do(transactions => Update(wallet, transactions, walletModel.AmountProvider))
			.Subscribe()
			.DisposeWith(_disposables);
	}

	public void Dispose()
	{
		_disposables.Dispose();
	}

	private void Update(Wallet wallet, TransactionModel[] transactions, IAmountProvider amountProvider)
	{
		// Number of coins in the wallet.
		CoinCount = wallet.Coins.Unspent().Count();

		// Total amount of money in the wallet.
		Balance = amountProvider.Create(wallet.Coins.TotalAmount());

		// Total amount of confirmed money in the wallet.
		ConfirmedBalance = amountProvider.Create(wallet.Coins.Confirmed().TotalAmount());

		// Total amount of unconfirmed money in the wallet.
		UnconfirmedBalance = amountProvider.Create(wallet.Coins.Unconfirmed().TotalAmount());

		GeneratedKeyCount = wallet.KeyManager.GetKeys().Count();
		GeneratedCleanKeyCount = wallet.KeyManager.GetKeys(KeyState.Clean).Count();
		GeneratedLockedKeyCount = wallet.KeyManager.GetKeys(KeyState.Locked).Count();
		GeneratedUsedKeyCount = wallet.KeyManager.GetKeys(KeyState.Used).Count();

		var singleCoinjoins =
			transactions
				.Where(x => x.Type == TransactionType.Coinjoin)
				.ToList();

		var groupedCoinjoins =
			transactions
				.Where(x => x.Type == TransactionType.CoinjoinGroup)
				.ToList();

		var nestedCoinjoins = groupedCoinjoins.SelectMany(x => x.Children).ToList();
		var nonCoinjoins =
			transactions
				.Where(x => !x.IsCoinjoin)
				.ToList();

		TotalTransactionCount = singleCoinjoins.Count + nestedCoinjoins.Count + nonCoinjoins.Count;
		NonCoinjointransactionCount = nonCoinjoins.Count;
		CoinjoinTransactionCount = singleCoinjoins.Count + nestedCoinjoins.Count;
	}
}
