using System.Collections;
using NBitcoin;
using System.Linq;
using System.Collections.Generic;
using WalletWasabi.Exceptions;
using WalletWasabi.Blockchain.TransactionOutputs;
using System.Collections.Immutable;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Extensions;
using WalletWasabi.Helpers;
using WalletWasabi.Models;

namespace WalletWasabi.Blockchain.TransactionBuilding;

public class SmartCoinSelector : ICoinSelector
{
	public SmartCoinSelector(List<SmartCoin> unspentCoins, int privateThreshold, SmartLabel recipient)
	{
		PrivateThreshold = privateThreshold;
		Recipient = recipient;
		UnspentCoins = unspentCoins.Distinct().ToList();
	}

	private List<SmartCoin> UnspentCoins { get; }
	private int IterationCount { get; set; }
	public int PrivateThreshold { get; }
	public SmartLabel Recipient { get; }

	/// <param name="suggestion">We use this to detect if NBitcoin tries to suggest something different and indicate the error.</param>
	/// <param name="target">Only <see cref="Money"/> type is really supported by this implementation.</param>
	/// <remarks>Do not call this method repeatedly on a single <see cref="SmartCoinSelector"/> instance.</remarks>
	public IEnumerable<ICoin> Select(IEnumerable<ICoin> suggestion, IMoney target)
	{
		var targetMoney = (Money)target;

		long available = UnspentCoins.Sum(x => x.Amount);
		if (available < targetMoney)
		{
			throw new InsufficientBalanceException(targetMoney, available);
		}

		if (IterationCount > 500)
		{
			throw new TimeoutException("Coin selection timed out.");
		}

		// The first iteration should never take suggested coins into account .
		if (IterationCount > 0)
		{
			Money suggestedSum = Money.Satoshis(suggestion.Sum(c => (Money)c.Amount));
			if (suggestedSum < targetMoney)
			{
				throw new TransactionSizeException(targetMoney, suggestedSum);
			}
		}

		// Get unique pockets.
		IEnumerable<Pocket> pockets = UnspentCoins.GetPockets(PrivateThreshold).ToImmutableArray();

		// Build all the possible pockets, except when it's computationally too expensive.
		List<Pocket> pocketCombinations = pockets.Count() < 10
			? pockets
				.CombinationsWithoutRepetition(ofLength: 1, upToLength: 6)
				.Select(pockets => pockets.Aggregate((current, pocket) => current + pocket))
				.ToList()
			: new List<Pocket>();

		var unspentCoinsPocket = pockets.Aggregate((current, pocket) => current + pocket);
		pocketCombinations.Add(unspentCoinsPocket);

		// This operation is doing super advanced grouping on the pockets and adding properties to each of them.
		var sayajinPockets = pocketCombinations
			.Select(pocket =>
			{
				var containedRecipientLabelsCount = pocket.Labels.Count(label => Recipient.Contains(label, StringComparer.OrdinalIgnoreCase));
				var totalPocketLabelsCount = pocket.Labels.Count();
				var totalRecipientLabelsCount = Recipient.Count();

				var index = ((double)containedRecipientLabelsCount / totalPocketLabelsCount) + ((double)containedRecipientLabelsCount / totalRecipientLabelsCount);
				var pocketPrivacy = 1.0m / totalPocketLabelsCount;

				return (Coins: pocket.Coins, RecipientIndex: index, PocketPrivacy: pocketPrivacy, NumberOfPockets: pocket.NumberOfCombinedPockets);
			})
			.Select(group => (
				Coins: group.Coins,
				Unconfirmed: group.Coins.Any(x => !x.Confirmed),    // If group has an unconfirmed, then the whole group is unconfirmed.
				AnonymitySet: group.Coins.Min(x => x.HdPubKey.AnonymitySet), // The group is as anonymous as its weakest member.
				PocketRecipientIndex: group.RecipientIndex, // An index for how acceptable is the pocket by taking the recipient into account. 0 -> Bad, 2 -> Perfect.
				PocketPrivacy: group.PocketPrivacy, // The number people/entities that know the pocket.
				NumberOfPockets: group.NumberOfPockets, // The number of how many pockets are combined into one.
				Amount: group.Coins.Sum(x => x.Amount)
			));

		// Find the best pocket combination that we are going to use.
		IEnumerable<SmartCoin> bestPocketCoins = sayajinPockets
			.Where(group => group.Amount >= targetMoney)
			.OrderBy(group => group.Unconfirmed)
			.ThenByDescending(group => group.AnonymitySet) // Always try to spend/merge the largest anonset coins first.
			.ThenByDescending(group => group.PocketRecipientIndex) // Select coins that known by the recipient.
			.ThenByDescending(group => group.PocketPrivacy) // Select lesser-known coins.
			.ThenBy(group => group.NumberOfPockets) // Avoid merging pockets as it is possible.
			.ThenByDescending(group => group.Amount) // Then always try to spend by amount.
			.First()
			.Coins; 

		var coinsInBestClusterByScript = bestPocketCoins
			.GroupBy(c => c.ScriptPubKey)
			.Select(group => (ScriptPubKey: group.Key, Coins: group.ToList()))
			.OrderBy(x => x.Coins.Sum(c => c.Amount))
			.ToImmutableList();

		// {1} {2} ... {n} {1, 2} {1, 2, 3} {1, 2, 3, 4} ... {1, 2, 3, 4, 5 ... n}
		var coinsGroup = coinsInBestClusterByScript.Select(x => ImmutableList.Create(x))
				.Concat(coinsInBestClusterByScript.Scan(ImmutableList<(Script ScriptPubKey, List<SmartCoin> Coins)>.Empty, (acc, coinGroup) => acc.Add(coinGroup)));

		// Flattens the groups of coins and filters out the ones that are too small.
		// Finally it sorts the solutions by number or coins (those with less coins on the top).
		var candidates = coinsGroup
			.Select(x => x.SelectMany(y => y.Coins))
			.Select(x => (Coins: x, Total: x.Sum(y => y.Amount)))
			.Where(x => x.Total >= targetMoney) // filter combinations below target
			.OrderBy(x => x.Coins.Count());

		IterationCount++;

		// Select the best solution.
		return candidates.First().Coins.Select(x => x.Coin);
	}
}
