using NSubstitute;
using StackExchange.Redis;

namespace PlayerBucket.Committer.Repositories.UnitTests;

[TestClass]
public class RedisRepositoryTests
{
	[TestMethod]
	public async Task Bid_會將token設定到Redis中()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();
		var token = "aaa";

		_ = fakeRedisDatabase.StringSetAsync(
			Arg.Is(new RedisKey($"Tgs:BucketCommit:{bucketId}")),
			Arg.Is(RedisValue.Unbox(token)),
			expiry: Arg.Is<TimeSpan?>(TimeSpan.FromMinutes(10D)),
			when: Arg.Is(When.NotExists))
			.Returns(true);

		var actual = await sut.BidAsync(bucketId, token);

		Assert.AreEqual(token, actual);
	}

	[TestMethod]
	public async Task Bid_如果已經有設定token_會回傳已經設定的token()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();
		var token = "aaa";

		_ = fakeRedisDatabase.StringSetAsync(
			Arg.Any<RedisKey>(),
			Arg.Any<RedisValue>())
			.Returns(false);

		_ = fakeRedisDatabase.StringGetAsync(Arg.Is(new RedisKey($"Tgs:BucketCommit:{bucketId}")))
			.Returns("bbb");

		var actual = await sut.BidAsync(bucketId, token);

		Assert.AreEqual("bbb", actual);
	}

	[TestMethod]
	public async Task GetBidToken_如果已經有token_會回傳已經設定的token()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();

		_ = fakeRedisDatabase.StringGetAsync(
			Arg.Is(new RedisKey($"Tgs:BucketCommit:{bucketId}")))
			.Returns("aaa");

		var actual = await sut.GetBidTokenAsync(bucketId);

		Assert.AreEqual("aaa", actual);
	}

	[TestMethod]
	public async Task GetBidToken_如果沒有token_會回傳空值()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();

		_ = fakeRedisDatabase.StringGetAsync(
			Arg.Is(new RedisKey($"Tgs:BucketCommit:{bucketId}")))
			.Returns(RedisValue.Null);

		var actual = await sut.GetBidTokenAsync(bucketId);

		Assert.IsNull(actual);
	}

	[TestMethod]
	public async Task DiscardBid_會刪除redis上的資料()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();

		await sut.DiscardBidAsync(bucketId);

		_ = fakeRedisDatabase.Received(1)
			.KeyDeleteAsync(Arg.Is(new RedisKey($"Tgs:BucketCommit:{bucketId}")));
	}

	[TestMethod]
	public async Task GetBucketAllEvents_取得Bucket的所有Events()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();

		_ = await sut.GetBucketAllEventsAsync(bucketId).ToArrayAsync();

		_ = fakeRedisDatabase.Received(1)
			.ListRangeAsync(Arg.Is(new RedisKey($"Tgs:Bucket:Events:{bucketId}")));
	}

	[TestMethod]
	public async Task RemoveEvents_移除已經處理完的Events()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var bucketId = Guid.NewGuid();
		var length = 10;

		await sut.RemoveEventsAsync(bucketId, length);

		_ = fakeRedisDatabase.Received(1)
			.ListLeftPopAsync(
				Arg.Is(new RedisKey($"Tgs:Bucket:Events:{bucketId}")),
				Arg.Is((long)length),
				Arg.Is(CommandFlags.FireAndForget));
	}

	[TestMethod]
	public async Task DeleteBucket_刪除已經快照完的Bucket暫存()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisRepository(fakeRedisDatabase);

		var playerCode = "test1";
		var bucketId = Guid.NewGuid();

		await sut.DeleteCreatedBucketAsync(playerCode, bucketId);

		_ = fakeRedisDatabase.Received(1)
			.HashDeleteAsync(
				Arg.Is(new RedisKey($"Tgs.Bucket:CreateBuckets:{playerCode}")),
				Arg.Is(new RedisValue(bucketId.ToString())));
	}
}
