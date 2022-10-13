using System.Text.Json;
using Json.Patch;
using NSubstitute;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;

namespace PlayerBucket.Worker.Core.UnitTests;

[TestClass]
public class BucketTests
{
	[TestMethod]
	public async Task Create_A_Bucket()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new CreateBucketCommandService(
			fakeTgsRepository,
			fakeSerialNumberGenerator);

		var command = new CreateBucketCommand(
			"test1",
			1000M,
			BucketType.Deposit);

		var createId = Guid.NewGuid();
		var expectedBucketInfo = new BucketCreateInfo(
			command.PlayerCode,
			command.Amount,
			command.BucketType);

		_ = fakeSerialNumberGenerator.GenerateAsync()
			.Returns(createId);

		_ = await sut.GetAsync(command);

		_ = fakeTgsRepository.Received(1)
			.AddPlayerBucketAsync(
				Arg.Is(createId),
				Arg.Is(createId),
				Arg.Is(expectedBucketInfo));
	}

	[TestMethod]
	public async Task Create_PlayerBucket_Snapshot()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new SnapshotPlayerBucketCommandService(fakeTgsRepository);

		var command = new SnapshotPlayerBucketCommand(Guid.NewGuid());

		var allEvents = new[] {
			new BucketEvent(
				Guid.NewGuid(),
				command.BucketId,
				1000M,
				FillType.Create,
				null,
				JsonDocument.Parse(new BucketCreateInfo(
					"test1",
					1000M,
					BucketType.Deposit).CreateJsonPatch(command.BucketId)),
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
				JsonDocument.Parse(new FillInBucketInfo(
					200M,
					FillType.Adjust,
					null,
					true).CreateJsonPatch()),
				DateTime.UtcNow)
		};
		_ = fakeTgsRepository.GetBucketAllEventsAsync(Arg.Is(command.BucketId))
			.Returns(allEvents.ToAsyncEnumerable());

		var expected = new BucketInfo(
			command.BucketId,
			"test1",
			1000M,
			BucketType.Deposit,
			0M,
			true,
			allEvents.Last().Id);

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.UpdatePlayerBucketAsync(
				Arg.Is(expected),
				450M);
	}

	[TestMethod]
	public void Create_JsonPatch_By_BucketCreateInfo()
	{
		var sut = new BucketCreateInfo("test1", 100M, BucketType.Deposit);

		var bucketId = Guid.NewGuid();
		var expected = new BucketInfo(
			bucketId,
			sut.PlayerCode,
			sut.Amount,
			sut.Type,
			0M,
			false,
			null);

		var patchJson = sut.CreateJsonPatch(bucketId);
		var patch = JsonSerializer.Deserialize<JsonPatch>(JsonDocument.Parse(patchJson));

		var actual = patch?.Apply<BucketInfo?>(null, PlayerBucketUtility.SerializerOptions);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void Create_JsonPatch_By_FillInBucketInfo()
	{
		var sut = new FillInBucketInfo(100M, FillType.Game, null, false);

		var patchJson = sut.CreateJsonPatch();
		var patch = JsonSerializer.Deserialize<JsonPatch>(JsonDocument.Parse(patchJson));

		var expected = new { IsFull = false };

		var actual = patch?.Apply(new { IsFull = false }, PlayerBucketUtility.SerializerOptions);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void Create_JsonPatch_By_FillInBucketInfo_SetFull_Is_True()
	{
		var sut = new FillInBucketInfo(100M, FillType.Game, null, true);

		var patchJson = sut.CreateJsonPatch();
		var patch = JsonSerializer.Deserialize<JsonPatch>(JsonDocument.Parse(patchJson));

		var expected = new { IsFull = true };

		var actual = patch?.Apply(new { IsFull = false }, PlayerBucketUtility.SerializerOptions);

		Assert.AreEqual(expected, actual);
	}
}
