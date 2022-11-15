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
				Guid.Parse("097c77de-8401-0000-0001-000080cd2019"),
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
				Guid.Parse("097c8e45-8401-0000-0001-000080cd2019"),
				command.BucketId,
				-100M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				DateTime.UtcNow),
			new BucketEvent(
				Guid.Parse("097cd6a5-8401-0000-0001-000080cd2019"),
				command.BucketId,
				-150M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				DateTime.UtcNow),
			new BucketEvent(
				Guid.Parse("097cd8ff-8401-0000-0001-000080cd2019"),
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
			450M,
			true,
			DateTime.MinValue,
			allEvents.Last().Id);

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.GetCreatedBucketSnapshotAsync(Arg.Is(command.BucketId));
		_ = fakeTgsRepository.Received(1)
			.UpdatePlayerBucketAsync(
				Arg.Is(expected));
		_ = fakeBucketEventRepository.Received(1)
			.DeleteCreatedBucketAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.BucketId));
	}

	[TestMethod]
	public async Task 建立Bucket快照_已經有中途的快照()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new SnapshotPlayerBucketCommandService(fakeTgsRepository, fakeBucketEventRepository);

		var command = new SnapshotPlayerBucketCommand(Guid.NewGuid(), "test1");

		var createdOn = DateTime.UtcNow;
		var allEvents = new[] {
			new BucketEvent(
				Guid.Parse("097c77de-8401-0000-0001-000080cd2019"),
				command.BucketId,
				1000M,
				FillType.Create,
				null,
				new BucketCreateInfo(
					command.PlayerCode,
					1000M,
					BucketType.Deposit,
					null).CreateJsonPatchDocument(command.BucketId),
				createdOn),
			new BucketEvent(
				Guid.Parse("097c8e45-8401-0000-0001-000080cd2019"),
				command.BucketId,
				-100M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				createdOn),
			new BucketEvent(
				Guid.Parse("097cd6a5-8401-0000-0001-000080cd2019"),
				command.BucketId,
				-150M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				createdOn),
			new BucketEvent(
				Guid.Parse("097cd8ff-8401-0000-0001-000080cd2019"),
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
				createdOn)
		};
		_ = fakeTgsRepository.GetBucketAllEventsAsync(
			Arg.Is(command.BucketId),
			Arg.Is(allEvents[0].Id))
			.Returns(allEvents.Skip(1).ToAsyncEnumerable());

		_ = fakeTgsRepository.GetCreatedBucketSnapshotAsync(Arg.Is(command.BucketId))
			.Returns(new BucketInfo(
				command.BucketId,
				command.PlayerCode,
				1000M,
				BucketType.Deposit,
				0M,
				false,
				createdOn,
				allEvents[0].Id));

		var expected = new BucketInfo(
			command.BucketId,
			command.PlayerCode,
			1000M,
			BucketType.Deposit,
			450M,
			true,
			createdOn,
			allEvents.Last().Id);

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.UpdatePlayerBucketAsync(
				Arg.Is(expected));
		_ = fakeBucketEventRepository.Received(1)
			.DeleteCreatedBucketAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.BucketId));
	}

	[TestMethod]
	public async Task 建立Bucket快照_沒有取得新的Events()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new SnapshotPlayerBucketCommandService(fakeTgsRepository, fakeBucketEventRepository);

		var command = new SnapshotPlayerBucketCommand(Guid.NewGuid(), "test1");

		var allEvents = new[] {
			new BucketEvent(
				Guid.Parse("097c77de-8401-0000-0001-000080cd2019"),
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
				Guid.Parse("097c8e45-8401-0000-0001-000080cd2019"),
				command.BucketId,
				-100M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				DateTime.UtcNow),
			new BucketEvent(
				Guid.Parse("097cd6a5-8401-0000-0001-000080cd2019"),
				command.BucketId,
				-150M,
				FillType.Game,
				null,
				JsonDocument.Parse("[]"),
				DateTime.UtcNow),
			new BucketEvent(
				Guid.Parse("097cd8ff-8401-0000-0001-000080cd2019"),
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
		_ = fakeTgsRepository.GetBucketAllEventsAsync(
			Arg.Is(command.BucketId),
			Arg.Is(allEvents[0].Id))
			.Returns(Array.Empty<BucketEvent>().ToAsyncEnumerable());

		_ = fakeTgsRepository.GetCreatedBucketSnapshotAsync(Arg.Is(command.BucketId))
			.Returns(new BucketInfo(
				command.BucketId,
				command.PlayerCode,
				1000M,
				BucketType.Deposit,
				250M,
				false,
				DateTime.Now,
				allEvents[2].Id));

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(0)
			.UpdatePlayerBucketAsync(
				Arg.Any<BucketInfo>());
		_ = fakeBucketEventRepository.Received(0)
			.DeleteCreatedBucketAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.BucketId));
	}
}
