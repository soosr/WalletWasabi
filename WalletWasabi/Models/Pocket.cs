using System.Linq;
using NBitcoin;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Extensions;

namespace WalletWasabi.Models;

public class Pocket
{
	public Pocket((SmartLabel labels, ICoinsView coins) pocket)
	{
		Coins = pocket.coins;
		Labels = pocket.labels;
		NumberOfCombinedPockets = 1;
	}

	public SmartLabel Labels { get; }

	public int NumberOfCombinedPockets { get; init; }

	public Money Amount => Coins.TotalAmount();

	public ICoinsView Coins { get; }

	public static Pocket Empty => new((SmartLabel.Empty, new CoinsView(Enumerable.Empty<SmartCoin>())));

	public Money EffectiveSumValue(FeeRate feeRate) => Coins.Sum(coin => coin.EffectiveValue(feeRate));

	public static Pocket Merge(params Pocket[] pockets)
	{
		var mergedLabels = SmartLabel.Merge(pockets.Select(p => p.Labels));
		var mergedCoins = new CoinsView(pockets.SelectMany(x => x.Coins).ToHashSet());
		var numberOfCombinations = pockets.Sum(p => p.NumberOfCombinedPockets);

		return new Pocket((mergedLabels, mergedCoins))
		{
			NumberOfCombinedPockets = numberOfCombinations
		};
	}

	public static Pocket Merge(Pocket[] pocketArray, params Pocket[] pockets)
	{
		var mergedPocketArray = Merge(pocketArray);
		var mergedPockets = Merge(pockets);

		return Merge(mergedPocketArray, mergedPockets);
	}
}
