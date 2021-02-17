using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.WabiSabi;
using WalletWasabi.WabiSabi.Backend;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.WabiSabi.Backend
{
	public class CoordinatorTests
	{
		[Fact]
		public async Task CanLiveAsync()
		{
			var workDir = Common.GetWorkDir();
			await IoHelpers.TryDeleteDirectoryAsync(workDir);
			CoordinatorParameters coordinatorParameters = new(workDir);
			using WabiSabiCoordinator coordinator = new(coordinatorParameters, new MockRpcClient());
			await coordinator.StartAsync(CancellationToken.None);
			await coordinator.StopAsync(CancellationToken.None);
		}

		[Fact]
		public async Task CanCancelAsync()
		{
			var workDir = Common.GetWorkDir();
			await IoHelpers.TryDeleteDirectoryAsync(workDir);
			CoordinatorParameters coordinatorParameters = new(workDir);

			using WabiSabiCoordinator coordinator = new(coordinatorParameters, new MockRpcClient());
			using CancellationTokenSource cts = new();
			cts.Cancel();
			await coordinator.StartAsync(cts.Token);
			await coordinator.StopAsync(CancellationToken.None);

			using WabiSabiCoordinator coordinator2 = new(coordinatorParameters, new MockRpcClient());
			using CancellationTokenSource cts2 = new();
			await coordinator2.StartAsync(cts2.Token);
			cts2.Cancel();
			await coordinator2.StopAsync(CancellationToken.None);

			using WabiSabiCoordinator coordinator3 = new(coordinatorParameters, new MockRpcClient());
			using CancellationTokenSource cts3 = new();
			var t = coordinator3.StartAsync(cts3.Token);
			cts3.Cancel();
			await t;
			await coordinator3.StopAsync(CancellationToken.None);

			using WabiSabiCoordinator coordinator4 = new(coordinatorParameters, new MockRpcClient());
			await coordinator4.StartAsync(CancellationToken.None);
			using CancellationTokenSource cts4 = new();
			cts4.Cancel();
			await coordinator4.StopAsync(cts4.Token);

			using WabiSabiCoordinator coordinator5 = new(coordinatorParameters, new MockRpcClient());
			await coordinator5.StartAsync(CancellationToken.None);
			using CancellationTokenSource cts5 = new();
			t = coordinator5.StopAsync(cts5.Token);
			cts5.Cancel();
			await t;
		}
	}
}
