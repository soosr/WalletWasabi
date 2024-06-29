using NBitcoin;
using NBitcoin.Secp256k1;
using NNostr.Client.Protocols;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace WalletWasabi.Discoverability;

public record AnnouncerConfig
{
	public bool IsEnabled { get; init; } = true;
	public string CoordinatorDescription { get; init; } = "WabiSabi Coinjoin Coordinator";
	public string CoordinatorUri { get; init; } = "https://api.example.com/";
	public decimal CoordinationFee { get; init; } = 0.0m;
	public uint AbsoluteMinInputCount { get; init; } = 21;
	public string ReadMoreUri { get; init; } = "https://api.example.com/";
	public string[] RelayUris { get; init;  } = ["wss://relay.primal.net"];
	public string Key { get; init; } = InitKey();

	private static string InitKey()
	{
		using var key = new Key();
		using var privKey = ECPrivKey.Create(key.ToBytes());
		return privKey.ToNIP19();
	}
}
