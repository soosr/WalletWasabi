﻿using DynamicData;
using WalletWasabi.Fluent.Models.Wallets;

namespace WalletWasabi.Fluent.Models.UI;

internal class NullWalletList : IWalletListModel
{
	public NullWalletList()
	{
		Wallets =
			Array.Empty<IWalletModel>()
				 .AsObservableChangeSet(x => x.Name);
	}

	public IObservable<IChangeSet<IWalletModel, string>> Wallets { get; }

	public IWalletModel? DefaultWallet { get; }
}