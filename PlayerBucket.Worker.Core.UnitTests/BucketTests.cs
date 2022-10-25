using Json.Patch;
using NSubstitute;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Worker.Core.UnitTests;

[TestClass]
public class BucketTests
{
	[TestMethod]
	public async Task CreateBucketCommand_建立Bucket()
	{
		var externalId = Guid.NewGuid().ToString();
		var sut = new CreateBucketCommand(100M, BucketType.Deposit, externalId);

		var fakeBucketStore = Substitute.For<IBucketStore>();

		await sut.ExecuteAsync(fakeBucketStore);

		_ = fakeBucketStore.Received(1)
			.CreateBucketAsync(
				Arg.Is(100M),
				Arg.Is(BucketType.Deposit),
				Arg.Is(externalId));
	}

	[TestMethod]
	public async Task FillInBucketCommand_填充Bucket_當Bucket的容量大於等於填充容量時_只會填充一次()
	{
		var externalId = Guid.NewGuid().ToString();
		var sut = new FillInBucketCommand(100M, FillType.Game, externalId);

		var fakeBucketStore = Substitute.For<IBucketStore>();

		var bucket = new BucketInfo(
			Guid.NewGuid(),
			"player1",
			150M,
			BucketType.Deposit,
			0M,
			false,
			null);

		_ = fakeBucketStore.PlayerCode
			.Returns(bucket.PlayerCode);

		_ = fakeBucketStore.GetNotFullBucketAsync()
			.Returns(bucket);

		var expectedFillInfo = new FillInBucketInfo(
			bucket.PlayerCode,
			100M,
			FillType.Game,
			externalId,
			false);

		await sut.ExecuteAsync(fakeBucketStore);

		await sut.WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeBucketStore.Received(1)
			.FillInBucketAsync(
				Arg.Is(bucket),
				Arg.Is(expectedFillInfo));
	}

	[TestMethod]
	public async Task FillInBucketCommand_填充Bucket_當Bucket的容量小於填充容量時_會找下一個Bucket填充()
	{
		var externalId = Guid.NewGuid().ToString();
		var sut = new FillInBucketCommand(100M, FillType.Game, externalId);

		var fakeBucketStore = Substitute.For<IBucketStore>();

		var firstBucket = new BucketInfo(
			Guid.NewGuid(),
			"player1",
			40M,
			BucketType.Deposit,
			0M,
			false,
			null);
		var secondBucket = new BucketInfo(
			Guid.NewGuid(),
			"player1",
			200M,
			BucketType.Deposit,
			0M,
			false,
			null);

		var returnBuckets = new Queue<BucketInfo>(new[] { firstBucket, secondBucket });

		_ = fakeBucketStore.PlayerCode
			.Returns("player1");

		_ = fakeBucketStore.GetNotFullBucketAsync()
			.Returns(_ => returnBuckets.Dequeue());

		var expectedFillInfo = new FillInBucketInfo(
			"player1",
			40M,
			FillType.Game,
			externalId,
			true);

		await sut.ExecuteAsync(fakeBucketStore);

		await sut.WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeBucketStore.Received(1)
			.FillInBucketAsync(
				Arg.Is(firstBucket),
				Arg.Is(expectedFillInfo));

		_ = fakeBucketStore.Received(1)
			.FillInBucketAsync(
				Arg.Is(secondBucket),
				Arg.Is(expectedFillInfo with
				{
					Amount = 60M,
					SetFull = false
				}));
	}

	[TestMethod]
	public async Task FillInBucketCommand_填充Bucket_如果沒有可以填充的Bucket_會結束填充()
	{
		var externalId = Guid.NewGuid().ToString();
		var sut = new FillInBucketCommand(100M, FillType.Game, externalId);

		var fakeBucketStore = Substitute.For<IBucketStore>();

		_ = fakeBucketStore.GetNotFullBucketAsync()
			.Returns(default(BucketInfo));

		await sut.ExecuteAsync(fakeBucketStore);

		await sut.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeBucketStore.Received(0)
			.FillInBucketAsync(
				Arg.Any<BucketInfo>(),
				Arg.Any<FillInBucketInfo>());
	}

	[TestMethod]
	public void BucketCreateInfo_產生JsonPatch字串()
	{
		var sut = new BucketCreateInfo("test1", 100M, BucketType.Deposit, null);

		var bucketId = Guid.NewGuid();
		var expected = new BucketInfo(
			bucketId,
			sut.PlayerCode,
			sut.Amount,
			sut.Type,
			0M,
			false,
			null);

		var patch = sut.CreateJsonPatch(bucketId);

		var actual = patch?.Apply<BucketInfo?>(null, PlayerBucketUtility.SerializerOptions);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void FillInBucketInfo_產生JsonPatch字串()
	{
		var sut = new FillInBucketInfo("test1", 100M, FillType.Game, null, false);

		var patch = sut.CreateJsonPatch();

		var expected = new { IsFull = false };

		var actual = patch?.Apply(new { IsFull = false }, PlayerBucketUtility.SerializerOptions);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void FillInBucketInfo_當SetFull為True時產生JsonPatch字串()
	{
		var sut = new FillInBucketInfo("test1", 100M, FillType.Game, null, true);

		var patch = sut.CreateJsonPatch();

		var expected = new { IsFull = true };

		var actual = patch?.Apply(new { IsFull = false }, PlayerBucketUtility.SerializerOptions);

		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public async Task ReadBucketAmountCommand_取得玩家的BucketAmount的值()
	{
		var sut = new ReadBucketAmountCommand();

		var fakeBucketStore = Substitute.For<IBucketStore>();

		_ = fakeBucketStore.GetBucketAmountAsync()
			.Returns(100M);

		await sut.ExecuteAsync(fakeBucketStore);

		var actual = await sut.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.AreEqual(100M, actual);
	}
}
