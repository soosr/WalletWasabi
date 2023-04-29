using NBitcoin;
using System.Linq;
using System.Collections.Generic;
using WalletWasabi.Exceptions;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.TransactionOutputs;
using System.Collections.Immutable;
using WalletWasabi.Extensions;
using WalletWasabi.Helpers;
using WalletWasabi.Models;

namespace WalletWasabi.Blockchain.TransactionBuilding;

public class SmartCoinSelector : ICoinSelector
{
	public SmartCoinSelector(List<SmartCoin> unspentCoins, SmartLabel recipient, int anonScoreTarget)
	{
		UnspentCoins = unspentCoins.Distinct().ToList();
		Recipient = recipient;
		AnonScoreTarget = anonScoreTarget;
	}

	private List<SmartCoin> UnspentCoins { get; }
	public SmartLabel Recipient { get; }
	public int AnonScoreTarget { get; }
	private int IterationCount { get; set; }
	private Exception? LastTransactionSizeException { get; set; }

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
			if (LastTransactionSizeException is not null)
			{
				throw LastTransactionSizeException;
			}

			throw new TimeoutException("Coin selection timed out.");
		}

		// The first iteration should never take suggested coins into account .
		if (IterationCount > 0)
		{
			Money suggestedSum = Money.Satoshis(suggestion.Sum(c => (Money)c.Amount));
			if (suggestedSum < targetMoney)
			{
				LastTransactionSizeException = new TransactionSizeException(targetMoney, suggestedSum);
			}
		}

		var pockets = UnspentCoins.ToPockets(AnonScoreTarget);
		var privacyOrderedPockets = pockets.OrderBy(GetPrivacyScore).ThenBy(x => x.Amount);
		var filteredPrivacyOrderedPockets = RemoveUnnecessaryUnconfirmedCoins(privacyOrderedPockets, targetMoney);
		var bestPockets = GetBestCombination(filteredPrivacyOrderedPockets, targetMoney);
		var bestPocketsCoins = bestPockets.Coins;

		var coinsInBestPocketByScript = bestPocketsCoins
			.GroupBy(c => c.ScriptPubKey)
			.Select(group => (ScriptPubKey: group.Key, Coins: group.ToList()))
			.OrderByDescending(x => x.Coins.Sum(c => c.Amount))
			.ToImmutableList();

		// {1} {2} ... {n} {1, 2} {1, 2, 3} {1, 2, 3, 4} ... {1, 2, 3, 4, 5 ... n}
		var coinsGroup = coinsInBestPocketByScript.Select(x => ImmutableList.Create(x))
			.Concat(coinsInBestPocketByScript.Scan(ImmutableList<(Script ScriptPubKey, List<SmartCoin> Coins)>.Empty, (acc, coinGroup) => acc.Add(coinGroup)));

		// Flattens the groups of coins and filters out the ones that are too small.
		// Finally it sorts the solutions by amount and coins (those with less coins on the top).
		var candidates = coinsGroup
			.Select(x => x.SelectMany(y => y.Coins))
			.Select(x => (Coins: x, Total: x.Sum(y => y.Amount)))
			.Where(x => x.Total >= targetMoney) // filter combinations below target
			.OrderBy(x => x.Total)              // the closer we are to the target the better
			.ThenBy(x => x.Coins.Count());      // prefer lesser coin selection on the same amount

		IterationCount++;

		// Select the best solution.
		return candidates.First().Coins.Select(x => x.Coin);
	}

	private Pocket GetBestCombination(IEnumerable<Pocket> filteredPrivacyOrderedPockets, Money targetMoney)
	{
		var best = filteredPrivacyOrderedPockets
			.CombinationsWithoutRepetition(ofLength: 1, upToLength: 6)
			.Select(pocketCombination => (Score: pocketCombination.Sum(GetPrivacyScore), Pocket: Pocket.Merge(pocketCombination.ToArray())))
			.Where(x => x.Pocket.Amount >= targetMoney)
			.OrderBy(x => x.Score)
			.ThenBy(x => x.Pocket.Amount)
			.First();

		return best.Pocket;
	}

	private IEnumerable<Pocket> RemoveUnnecessaryUnconfirmedCoins(IOrderedEnumerable<Pocket> privacyOrderedPockets, Money targetMoney)
	{
		var list = new List<Pocket>();

		foreach (var pocket in privacyOrderedPockets.Reverse())
		{
			var allOtherPocketAmount = privacyOrderedPockets.Where(x => x != pocket).Sum(x => x.Amount);
			var pocketConfirmedAmount = pocket.Coins.Confirmed().TotalAmount();

			if (allOtherPocketAmount + pocketConfirmedAmount >= targetMoney)
			{
				var confirmedCoins = pocket.Coins.Confirmed();
				var label = new SmartLabel(confirmedCoins.SelectMany(x => x.HdPubKey.Cluster.Labels));
				list.Add(new Pocket((label, confirmedCoins)));
			}
			else
			{
				list.Add(pocket);
			}
		}

		list.Reverse();

		return list;
	}

	private IEnumerable<Pocket> RemoveUnnecessaryPockets(IEnumerable<Pocket> filteredPrivacyOrderedPockets, Money targetMoney)
	{
		var pocketCandidates = new List<Pocket>();

		foreach (var pocket in filteredPrivacyOrderedPockets)
		{
			pocketCandidates.Add(pocket);

			if (pocketCandidates.Sum(x => x.Amount) >= targetMoney)
			{
				break;
			}
		}

		foreach (var pocket in pocketCandidates.OrderBy(x => x.Amount).ToImmutableArray())
		{
			if (pocketCandidates.Except(new[] { pocket }).Sum(x => x.Amount) >= targetMoney)
			{
				pocketCandidates.Remove(pocket);
			}
			else
			{
				break;
			}
		}

		return pocketCandidates;
	}

	private decimal GetPrivacyScore(Pocket pocket)
	{
		if (Recipient.Equals(pocket.Labels, StringComparer.OrdinalIgnoreCase))
		{
			return 1;
		}

		if (pocket.IsPrivate(AnonScoreTarget))
		{
			return 2;
		}

		if (pocket.IsSemiPrivate(AnonScoreTarget, Constants.SemiPrivateThreshold))
		{
			return 3;
		}

		if (pocket.IsUnknown())
		{
			return 7;
		}

		var containedRecipientLabelsCount = pocket.Labels.Count(label => Recipient.Contains(label, StringComparer.OrdinalIgnoreCase));
		if (containedRecipientLabelsCount > 0)
		{
			var index = ((decimal)containedRecipientLabelsCount / pocket.Labels.Count) + ((decimal)containedRecipientLabelsCount / Recipient.Count);
			return 4 + (2 - index);
		}

		var x = 6 + 1 - (1M / pocket.Labels.Count);
		return x;
	}
}
