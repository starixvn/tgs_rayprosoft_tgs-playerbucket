using NSubstitute;
using StackExchange.Redis;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.Repositories;

namespace PlayerBucket.Repositories.UnitTests;

[TestClass]
public class RedisDistributedResourceManagerTests
{
	[TestMethod]
	public async Task Bid_會將token設定到Redis中()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";
		var token = "aaa";

		_ = fakeRedisDatabase.StringSetAsync(
			Arg.Is(new RedisKey($"Tgs:Bucket:{playerCode}")),
			Arg.Is(RedisValue.Unbox(token)),
			expiry: Arg.Is<TimeSpan?>(TimeSpan.FromMinutes(10D)),
			when: Arg.Is(When.NotExists))
			.Returns(true);

		var actual = await sut.BidAsync(playerCode, token);

		Assert.AreEqual(token, actual);
	}

	[TestMethod]
	public async Task Bid_如果已經有設定token_會回傳已經設定的token()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";
		var token = "aaa";

		_ = fakeRedisDatabase.StringSetAsync(
			Arg.Any<RedisKey>(),
			Arg.Any<RedisValue>())
			.Returns(false);

		_ = fakeRedisDatabase.StringGetAsync(Arg.Is(new RedisKey($"Tgs:Bucket:{playerCode}")))
			.Returns("bbb");

		var actual = await sut.BidAsync(playerCode, token);

		Assert.AreEqual("bbb", actual);
	}

	[TestMethod]
	public async Task GetBidToken_如果已經有token_會回傳已經設定的token()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";

		_ = fakeRedisDatabase.StringGetAsync(
			Arg.Is(new RedisKey($"Tgs:Bucket:{playerCode}")))
			.Returns("aaa");

		var actual = await sut.GetBidTokenAsync(playerCode);

		Assert.AreEqual("aaa", actual);
	}

	[TestMethod]
	public async Task GetBidToken_如果沒有token_會回傳空值()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";

		_ = fakeRedisDatabase.StringGetAsync(
			Arg.Is(new RedisKey($"Tgs:Bucket:{playerCode}")))
			.Returns(RedisValue.Null);

		var actual = await sut.GetBidTokenAsync(playerCode);

		Assert.IsNull(actual);
	}

	[TestMethod]
	public async Task DiscardBid_會刪除redis上的資料以解鎖()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";

		await sut.DiscardBidAsync(playerCode);

		_ = fakeRedisDatabase.Received(1)
			.KeyDeleteAsync(Arg.Is(new RedisKey($"Tgs:Bucket:{playerCode}")));
	}

	[TestMethod]
	public async Task ExtendBid_延長處理時間()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";

		_ = await sut.ExtendBidAsync(playerCode);

		_ = fakeRedisDatabase.Received(1)
			.KeyExpireAsync(
				Arg.Is(new RedisKey($"Tgs:Bucket:{playerCode}")),
				Arg.Is(TimeSpan.FromMinutes(10D)),
				Arg.Is(CommandFlags.FireAndForget));
	}

	[TestMethod]
	public async Task AddPlayerBucket_新增一個Bucket()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var createInfo = new BucketCreateInfo(
			"test1",
			100M,
			BucketType.Deposit,
			null);
		var createId = Guid.NewGuid();
		var bucketId = Guid.NewGuid();

		await sut.AddPlayerBucketAsync(createId, bucketId, createInfo, null);

		_ = fakeRedisDatabase.Received(1)
			.ListRightPushAsync(
				Arg.Is(new RedisKey($"Tgs:Bucket:Events:{bucketId}")),
				Arg.Any<RedisValue>());

		_ = fakeRedisDatabase.Received(1)
			.HashSetAsync(
				Arg.Is(new RedisKey($"Tgs:Bucket:CreateBuckets:{createInfo.PlayerCode}")),
				Arg.Is(new RedisValue(bucketId.ToString())),
				Arg.Any<RedisValue>());
	}

	[TestMethod]
	public async Task GetBucketAllEvents_取出Bucket的所有Events()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();

		_ = await sut.GetBucketAllEventsAsync(bucketId).ToArrayAsync();

		_ = fakeRedisDatabase.Received(1)
			.ListRangeAsync(Arg.Is(new RedisKey($"Tgs:Bucket:Events:{bucketId}")));
	}

	[TestMethod]
	public async Task GetBucketAllEvents_依照SnapshotId取出Bucket的Evetns()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();
		var snapshotId = Guid.NewGuid();

		_ = await sut.GetBucketAllEventsAsync(bucketId, snapshotId).ToArrayAsync();

		_ = fakeRedisDatabase.Received(1)
			.ListRangeAsync(Arg.Is(new RedisKey($"Tgs:Bucket:Events:{bucketId}")));
	}

	[TestMethod]
	public async Task FillInBucket_填充Bucket()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var createId = Guid.NewGuid();
		var bucketId = Guid.NewGuid();
		var fillInfo = new FillInBucketInfo(
			"test1",
			10M,
			FillType.Game,
			null,
			false);

		await sut.FillInBucketAsync(createId, bucketId, fillInfo, null);

		_ = fakeRedisDatabase.Received(1)
			.ListRightPushAsync(
				Arg.Is(new RedisKey($"Tgs:Bucket:Events:{bucketId}")),
				Arg.Any<RedisValue>());
	}

	[TestMethod]
	public async Task GetPlayerCreatedBuckets_取得玩家建立尚未寫入db的Buckets()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";

		_ = await sut.GetPlayerCreatedBucketsAsync(playerCode).ToArrayAsync();

		_ = fakeRedisDatabase.Received(1)
			.HashGetAllAsync(Arg.Is(new RedisKey($"Tgs.Bucket:CreateBuckets:{playerCode}")));
	}
}
