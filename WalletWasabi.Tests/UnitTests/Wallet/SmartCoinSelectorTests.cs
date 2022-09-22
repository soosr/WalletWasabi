using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.Tests.UnitTests.UserInterfaceTest;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Wallet;

public class SmartCoinSelectorTests
{
	public SmartCoinSelectorTests()
	{
		KeyManager = KeyManager.Recover(
			new Mnemonic("all all all all all all all all all all all all"),
			"",
			Network.Main,
			KeyManager.GetAccountKeyPath(Network.Main));
	}

	private KeyManager KeyManager { get; }

	[Fact]
	public void SelectsOnlyOneCoinWhenPossible()
	{
		var availableCoins = GenerateSmartCoins(
			Enumerable.Range(0, 9).Select(i => ("Juan", 0.1m * (i + 1))))
			.ToList();

		var selector = new SmartCoinSelector(availableCoins, privateThreshold: 100, recipient: SmartLabel.Empty);
		var coinsToSpend = selector.Select(Enumerable.Empty<Coin>(), Money.Coins(0.3m));

		var theOnlyOne = Assert.Single(coinsToSpend.Cast<Coin>());
		Assert.Equal(0.3m, theOnlyOne.Amount.ToUnit(MoneyUnit.BTC));
	}

	[Fact]
	public void PreferLessCoinsOverExactAmount()
	{
		var smartCoins = GenerateSmartCoins(
			Enumerable.Range(0, 10).Select(i => ("Juan", 0.1m * (i + 1))))
			.ToList();

		smartCoins.Add(BitcoinFactory.CreateSmartCoin(smartCoins[0].HdPubKey, 0.11m));

		var selector = new SmartCoinSelector(smartCoins, privateThreshold: 100, recipient: SmartLabel.Empty);

		var someCoins = smartCoins.Select(x => x.Coin);
		var coinsToSpend = selector.Select(someCoins, Money.Coins(0.41m));

		var theOnlyOne = Assert.Single(coinsToSpend.Cast<Coin>());
		Assert.Equal(0.5m, theOnlyOne.Amount.ToUnit(MoneyUnit.BTC));
	}

	[Fact]
	public void PreferSameScript()
	{
		var smartCoins = GenerateSmartCoins(Enumerable.Repeat(("Juan", 0.2m), 12)).ToList();

		smartCoins.Add(BitcoinFactory.CreateSmartCoin(smartCoins[0].HdPubKey, 0.11m));

		var selector = new SmartCoinSelector(smartCoins, privateThreshold: 100, recipient: SmartLabel.Empty);

		var coinsToSpend = selector.Select(Enumerable.Empty<Coin>(), Money.Coins(0.31m)).Cast<Coin>().ToList();

		Assert.Equal(2, coinsToSpend.Count);
		Assert.Equal(coinsToSpend[0].ScriptPubKey, coinsToSpend[1].ScriptPubKey);
		Assert.Equal(0.31m, coinsToSpend.Sum(x => x.Amount.ToUnit(MoneyUnit.BTC)));
	}

	[Fact]
	public void PreferMorePrivateClusterScript()
	{
		var coinsKnownByJuan = GenerateSmartCoins(Enumerable.Repeat(("Juan", 0.2m), 5));

		var coinsKnownByBeto = GenerateSmartCoins(Enumerable.Repeat(("Beto", 0.2m), 2));

		var selector = new SmartCoinSelector(coinsKnownByJuan.Concat(coinsKnownByBeto).ToList(), privateThreshold: 100, recipient: SmartLabel.Empty);
		var coinsToSpend = selector.Select(Enumerable.Empty<Coin>(), Money.Coins(0.3m)).Cast<Coin>().ToList();

		Assert.Equal(2, coinsToSpend.Count);
		Assert.Equal(0.4m, coinsToSpend.Sum(x => x.Amount.ToUnit(MoneyUnit.BTC)));
	}

	[Fact]
	public void PreferPrivateCoins()
	{
		var privateThreshold = 100;
		var amountToSend = Money.Coins(0.6m);

		var smartCoins = new List<SmartCoin>()
		{
			LabelTestExtensions.CreateCoin(0.5m, "Juan", anonymitySet: 150),
			LabelTestExtensions.CreateCoin(0.3m, "Beto", anonymitySet: 150),
			LabelTestExtensions.CreateCoin(0.8m, "Juan", anonymitySet: 1),
			LabelTestExtensions.CreateCoin(0.6m, "Beto", anonymitySet: 1),
		};
		var privateCoins = smartCoins.Where(x => x.HdPubKey.AnonymitySet > privateThreshold).Select(x => x.Coin);

		var selector = new SmartCoinSelector(smartCoins, privateThreshold, recipient: "");
		var coinsToSpend = selector.Select(Enumerable.Empty<Coin>(), amountToSend).Cast<Coin>().ToList();

		Assert.True(coinsToSpend.All(coin => privateCoins.Contains(coin)));
	}

	[Fact]
	public void PreferSemiPrivateCoins()
	{
		var privateThreshold = 100;
		var amountToSend = Money.Coins(1.0m);

		var smartCoins = new List<SmartCoin>()
		{
			LabelTestExtensions.CreateCoin(0.5m, "Juan", anonymitySet: 150),
			LabelTestExtensions.CreateCoin(0.3m, "Beto", anonymitySet: 5),
			LabelTestExtensions.CreateCoin(0.8m, "Juan", anonymitySet: 5),
			LabelTestExtensions.CreateCoin(1.6m, "Beto", anonymitySet: 1),
		};
		var semiPrivateCoins = smartCoins.Where(x => x.HdPubKey.AnonymitySet < privateThreshold && x.HdPubKey.AnonymitySet >= 2).Select(x => x.Coin);

		var selector = new SmartCoinSelector(smartCoins, privateThreshold, recipient: "");
		var coinsToSpend = selector.Select(Enumerable.Empty<Coin>(), amountToSend).Cast<Coin>().ToList();

		Assert.True(coinsToSpend.All(coin => semiPrivateCoins.Contains(coin)));
	}

	[Fact]
	public void PreferSemiPrivateAndPrivateCoins()
	{
		var privateThreshold = 100;
		var amountToSend = Money.Coins(0.6m);

		var smartCoins = new List<SmartCoin>()
		{
			LabelTestExtensions.CreateCoin(0.5m, "Juan", anonymitySet: 150),
			LabelTestExtensions.CreateCoin(0.3m, "Beto", anonymitySet: 5),
			LabelTestExtensions.CreateCoin(0.8m, "Juan", anonymitySet: 1),
			LabelTestExtensions.CreateCoin(1.6m, "Beto", anonymitySet: 1),
			LabelTestExtensions.CreateCoin(0.3m, "Beto", anonymitySet: 1),
		};
		var semiPrivateAndPrivateCoins = smartCoins.Where(x => x.HdPubKey.AnonymitySet >= 2).Select(x => x.Coin);

		var selector = new SmartCoinSelector(smartCoins, privateThreshold, recipient: "");
		var coinsToSpend = selector.Select(Enumerable.Empty<Coin>(), amountToSend).Cast<Coin>().ToList();

		Assert.True(coinsToSpend.All(coin => semiPrivateAndPrivateCoins.Contains(coin)));
	}

	[Theory]
	[InlineData("Lucas")]
	[InlineData("David")]
	[InlineData("Lucas, David")]
	public void PreferCoinsThatKnownByTheRecipient(SmartLabel recipient)
	{
		var privateThreshold = 100;
		var amountToSend = Money.Coins(0.5m);

		var smartCoins = new List<SmartCoin>()
		{
			LabelTestExtensions.CreateCoin(1m, "Juan"),
			LabelTestExtensions.CreateCoin(1m, "Beto"),
			LabelTestExtensions.CreateCoin(1m, "Lucas"),
			LabelTestExtensions.CreateCoin(1m, "David"),
			LabelTestExtensions.CreateCoin(1m, "Lucas, David"),
		};
		var knownByRecipientCoins = smartCoins.Where(x => x.HdPubKey.Label == recipient).Select(x => x.Coin);

		var selector = new SmartCoinSelector(smartCoins, privateThreshold, recipient);
		var coinsToSpend = selector.Select(Enumerable.Empty<Coin>(), amountToSend).Cast<Coin>().ToList();

		Assert.True(coinsToSpend.All(coin => knownByRecipientCoins.Contains(coin)));
	}

	private IEnumerable<SmartCoin> GenerateSmartCoins(IEnumerable<(string Cluster, decimal amount)> coins)
	{
		Dictionary<string, List<(HdPubKey key, decimal amount)>> generatedKeyGroup = new();

		// Create cluster-grouped keys
		foreach (var targetCoin in coins)
		{
			var key = KeyManager.GenerateNewKey(new SmartLabel(targetCoin.Cluster), KeyState.Clean, false);

			if (!generatedKeyGroup.ContainsKey(targetCoin.Cluster))
			{
				generatedKeyGroup.Add(targetCoin.Cluster, new());
			}

			generatedKeyGroup[targetCoin.Cluster].Add((key, targetCoin.amount));
		}

		var coinPairClusters = generatedKeyGroup.GroupBy(x => x.Key)
			.Select(x => x.Select(y => y.Value)) // Group the coin pairs into clusters.
			.SelectMany(x => x
				.Select(coinPair => (coinPair,
					cluster: new Cluster(coinPair.Select(z => z.key))))).ToList();

		// Set each key with its corresponding cluster object.
		foreach (var x in coinPairClusters)
		{
			foreach (var y in x.coinPair)
			{
				y.key.Cluster = x.cluster;
			}
		}

		return coinPairClusters.Select(x => x.coinPair)
			.SelectMany(x =>
				x.Select(y => BitcoinFactory.CreateSmartCoin(y.key, y.amount))); // Generate the final SmartCoins.
	}
}
