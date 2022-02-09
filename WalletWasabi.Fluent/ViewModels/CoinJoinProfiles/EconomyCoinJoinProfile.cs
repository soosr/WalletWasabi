using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WalletWasabi.Fluent.Helpers;

namespace WalletWasabi.Fluent.ViewModels.CoinJoinProfiles;

internal class EconomyCoinJoinProfile : CoinJoinProfileViewModel
{
	public override string Title => "Speedy";

	public override string Description => "very speedy";

	public override IImage Icon => AssetHelpers.GetBitmapAsset($"avares://WalletWasabi.Fluent/Assets/PasswordFinder/{ThemeHelper.CurrentTheme}/numbers.png");

	public override int FeeTargetAvarageTimeFrameHours => 168; // One week avarage.
}
