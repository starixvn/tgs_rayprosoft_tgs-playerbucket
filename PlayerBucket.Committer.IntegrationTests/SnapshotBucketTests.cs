using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.ApplicationServices;
using Tgs.PlayerBuckets.Committer.MessageQueue;
using Tgs.Services.PlayerBucket.Protos;
using Tgs.ServiceUtility;

namespace PlayerBucket.Committer.IntegrationTests;

[TestClass]
public class SnapshotBucketTests
{
	[TestMethod]
	public async Task BucketCommitterDispatcher_處理快照要求可以正確傳遞()
	{
		var fakeSnapshotCommandService = Substitute.For<ICommandService<SnapshotPlayerBucketCommand>>();
		var fakeDistributedResourceManager = Substitute.For<IDistributedResourceManager>();

		await using var web = PlayerBucketCommitterTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddSingleton(fakeSnapshotCommandService)
					.AddFakeNatsMessageQueue()
					.AddSingleton(fakeDistributedResourceManager)));

		var sut = ActivatorUtilities.CreateInstance<SnapshotBucketHandler>(web.Services);

		var request = new SnapshotRequest
		{
			BucketId = Guid.NewGuid(),
			PlayerCode = "test1"
		};

		var tcs = new TaskCompletionSource();

		var expected = new SnapshotPlayerBucketCommand(request.BucketId, request.PlayerCode);

		fakeSnapshotCommandService
			.When(fake => fake.ExecuteAsync(
				Arg.Is(expected),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs.SetResult());

		await sut.HandleAsync(request.ToByteArray());

		await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3D));
	}
}
