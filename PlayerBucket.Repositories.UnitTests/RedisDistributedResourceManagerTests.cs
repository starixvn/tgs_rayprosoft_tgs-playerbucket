using NSubstitute;
using StackExchange.Redis;
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
	public async Task DiscardBid_會刪除redis上的資料()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();

		var sut = new RedisDistributedResourceManager(fakeRedisDatabase);

		var playerCode = "test1";

		await sut.DiscardBidAsync(playerCode);

		_ = fakeRedisDatabase.Received(1)
			.KeyDeleteAsync(Arg.Is(new RedisKey($"Tgs:Bucket:{playerCode}")));
	}
}
