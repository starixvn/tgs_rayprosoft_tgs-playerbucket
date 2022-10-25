using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using NSubstitute;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.ApplicationServices;
using Tgs.PlayerBuckets.Committer.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace PlayerBucket.Committer.IntegrationTests;

[TestClass]
public class ProcessBucketEventsTests
{
	[TestMethod]
	public async Task BucketCommitterDispatcher_處理Bucket的要求可以正確傳遞並處理()
	{
		var fakeProcessBucketEventsCommandService = Substitute.For<ICommandService<ProcessBucketEventsCommand>>();
		var fakeDistributedResourceManager = Substitute.For<IDistributedResourceManager>();

		await using var web = PlayerBucketCommitterTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddSingleton(fakeProcessBucketEventsCommandService)
					.AddFakeNatsMessageQueue()
					.AddSingleton(fakeDistributedResourceManager)));

		var sut = ActivatorUtilities.CreateInstance<ProcessBucketEventRequestHandler>(web.Services);

		var request = new BucketChangedEvent(Guid.NewGuid(), "test1");

		var tcs = new TaskCompletionSource();

		var expected = new ProcessBucketEventsCommand(request.BucketId, request.PlayerCode);

		fakeProcessBucketEventsCommandService
			.When(fake => fake.ExecuteAsync(
				Arg.Is(expected),
				Arg.Any<CancellationToken>()))
			.Do(_ => tcs.SetResult());

		await sut.HandleAsync(request.ToBson());

		await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3D));
	}
}
