using System.Text.Json;
using NSubstitute;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.ApplicationServices;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Committer.Core.UnitTests;

[TestClass]
public class BucketTests
{
	[TestMethod]
	public async Task 建立Bucket快照()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new SnapshotPlayerBucketCommandService(fakeTgsRepository, fakeBucketEventRepository);

		var command = new SnapshotPlayerBucketCommand(Guid.NewGuid(), "test1");

		var allEvents = new[] {
			new BucketEvent(
				Guid.NewGuid(),
				command.BucketId,
				1000M,
				FillType.Create,
				null,
				new BucketCreateInfo(
					command.PlayerCode,
					1000M,
					BucketType.Deposit,
					null).CreateJsonPatchDocument(command.BucketId),
				DateTime.UtcNow),
			new BucketEvent(
				Guid.NewGuid(),
				command.BucketId,
				-100M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				DateTime.UtcNow),
			new BucketEvent(
				Guid.NewGuid(),
				command.BucketId,
				-150M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				DateTime.UtcNow),
			new BucketEvent(
				Guid.NewGuid(),
				command.BucketId,
				-200M,
				FillType.Game,
				null,
				new FillInBucketInfo(
					command.PlayerCode,
					200M,
					FillType.Adjust,
					null,
					true).CreateJsonPatchDocument(),
				DateTime.UtcNow)
		};
		_ = fakeTgsRepository.GetBucketAllEventsAsync(Arg.Is(command.BucketId))
				.Returns(allEvents.ToAsyncEnumerable());

		var expected = new BucketInfo(
			command.BucketId,
			command.PlayerCode,
			1000M,
			BucketType.Deposit,
			0M,
			true,
			allEvents.Last().Id);

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.GetCreatedBucketSnapshotAsync(Arg.Is(command.BucketId));
		_ = fakeTgsRepository.Received(1)
			.UpdatePlayerBucketAsync(
				Arg.Is(expected),
				Arg.Is(450M));
		_ = fakeBucketEventRepository.Received(1)
			.DeleteCreatedBucketAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.BucketId));
	}
}
