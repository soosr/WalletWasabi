﻿using System.Collections.Generic;
using WalletWasabi.BitcoinCore.Rpc;
using WalletWasabi.Extensions;
using WalletWasabi.Helpers;
using WalletWasabi.Hwi.Exceptions;

namespace WalletWasabi.Fluent.Extensions;

public static class UserFriendlyExceptionExtensions
{
	public static string ToUserFriendlyString(this Exception ex)
	{
		var trimmed = Guard.Correct(ex.Message);
		if (trimmed.Length == 0)
		{
			return ex.ToTypeMessageString();
		}
		else
		{
			if (ex is OperationCanceledException exception)
			{
				return exception.Message;
			}
			else if (ex is HwiException hwiEx)
			{
				if (hwiEx.ErrorCode == HwiErrorCode.DeviceConnError)
				{
					return "Could not find the hardware wallet.\nMake sure it is connected.";
				}
				else if (hwiEx.ErrorCode == HwiErrorCode.ActionCanceled)
				{
					return "The transaction was canceled on the device.";
				}
				else if (hwiEx.ErrorCode == HwiErrorCode.UnknownError)
				{
					return "Unknown error.\nMake sure the device is connected and isn't busy, then try again.";
				}
			}

			foreach (KeyValuePair<string, string> pair in RpcErrorTools.ErrorTranslations)
			{
				if (trimmed.Contains(pair.Key, StringComparison.InvariantCultureIgnoreCase))
				{
					return pair.Value;
				}
			}

			return ex.ToTypeMessageString();
		}
	}
}