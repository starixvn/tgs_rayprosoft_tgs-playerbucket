using NSubstitute;
using Tgs.PlayerBucket.Client;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucketType.Client.UnitTests;

public class PlayerBucketForTypesClientTests
{
	[Fact]
	public async Task 建立含遊戲類型的PlayerBuckets()
	{
		// Arrange
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

		var sut = new PlayerBucketForTypesClient(fakePlayerBucketClient);

		var playerCode = "test1";
		var bucketTypes = new[] { "Game1", "Game2" };

		// Act
		await sut.CreateBucketForBucketTypesAsync(
			playerCode,
			1000M,
			BucketType.Deposit,
			null,
			null,
			bucketTypes);

		// Assert
		_ = fakePlayerBucketClient.Received(1)
			.CreateBucketAsync(
				Arg.Is(playerCode),
				Arg.Is(1000M),
				Arg.Is(BucketType.Deposit),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(1)
			.CreateBucketAsync(
				Arg.Is($"{playerCode}:Game1"),
				Arg.Is(1000M),
				Arg.Is(BucketType.Deposit),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(1)
			.CreateBucketAsync(
				Arg.Is($"{playerCode}:Game2"),
				Arg.Is(1000M),
				Arg.Is(BucketType.Deposit),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(3)
			.CreateBucketAsync(
				Arg.Any<string>(),
				Arg.Any<decimal>(),
				Arg.Any<BucketType>(),
				Arg.Any<string>(),
				Arg.Any<string>());
	}

	[Fact]
	public async Task 填入遊戲類型流水()
	{
		// Arrange
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

		var sut = new PlayerBucketForTypesClient(fakePlayerBucketClient);

		var playerCode = "test1";
		var gameType = "Game1";

		// Act
		await sut.FillInBucketForBucketTypesAsync(
			playerCode,
			100M,
			FillType.Game,
			null,
			null,
			new[] { gameType });

		// Assert
		_ = fakePlayerBucketClient.Received(1)
			.FillInBucketAsync(
				Arg.Is(playerCode),
				Arg.Is(100M),
				Arg.Is(FillType.Game),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(1)
			.FillInBucketAsync(
				Arg.Is($"{playerCode}:{gameType}"),
				Arg.Is(100M),
				Arg.Is(FillType.Game),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(2)
			.FillInBucketAsync(
				Arg.Any<string>(),
				Arg.Any<decimal>(),
				Arg.Any<FillType>(),
				Arg.Any<string>(),
				Arg.Any<string>());
	}

	[Fact]
	public async Task 讀取所有類型水桶餘額()
	{
		// Arrange
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

		var sut = new PlayerBucketForTypesClient(fakePlayerBucketClient);

		var playerCode = "test1";
		var gameTypes = new[] { "Game1", "Game2" };

		// Act
		_ = await sut.ReadBucketsForBucketTypesAsync(playerCode, gameTypes).ToArrayAsync();

		// Assert
		_ = fakePlayerBucketClient.Received(1)
			.ReadBucketsAsync(Arg.Is(playerCode));
		_ = fakePlayerBucketClient.Received(1)
			.ReadBucketsAsync(Arg.Is($"{playerCode}:Game1"));
		_ = fakePlayerBucketClient.Received(1)
			.ReadBucketsAsync(Arg.Is($"{playerCode}:Game2"));
		_ = fakePlayerBucketClient.Received(3)
			.ReadBucketsAsync(Arg.Any<string>());
	}
}
