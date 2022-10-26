using System.Text.Json;
using NSubstitute;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.ApplicationServices;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Committer.Core.UnitTests;

[TestClass]
public class BucketEventProcessTests
{
	[TestMethod]
	public async Task ProcessBucketEvents_處理事件批次()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();
		var fakeEventSource = Substitute.For<IEventSource>();

		var sut = new ProcessBucketEventsCommandService(
			fakeTgsRepository,
			fakeBucketEventRepository,
			fakeEventSource);

		var command = new ProcessBucketEventsCommand(Guid.NewGuid(), "test1");

		var eventBatchQueue = new Queue<BucketEvent[]>(new[] {
			new[]{
				new BucketEvent(
					Guid.NewGuid(),
					command.BucketId,
					-10M,
					FillType.Game,
					Guid.NewGuid().ToString(),
					JsonDocument.Parse("[]"),
					DateTime.UtcNow)
			},
			Array.Empty<BucketEvent>()
		});

		_ = fakeBucketEventRepository.GetWaitingInsertEventsAsync(Arg.Is(command.BucketId))
			.Returns(_ => eventBatchQueue.Dequeue().ToAsyncEnumerable());

		await sut.ExecuteAsync(command);

		_ = fakeBucketEventRepository.Received(2)
			.GetWaitingInsertEventsAsync(Arg.Is(command.BucketId));
		_ = fakeTgsRepository.Received(1)
			.InsertBucketEventsAsync(Arg.Any<IEnumerable<BucketEvent>>());
		_ = fakeBucketEventRepository.Received(1)
			.RemoveEventsAsync(Arg.Is(command.BucketId), Arg.Any<int>());
	}
}
