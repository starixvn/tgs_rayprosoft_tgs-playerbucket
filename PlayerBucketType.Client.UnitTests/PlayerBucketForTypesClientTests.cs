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

		var normalResult = new ReadBucketResult(100M, Guid.NewGuid());
		var game1Result = new ReadBucketResult(200M, Guid.NewGuid());
		var game2Result = new ReadBucketResult(300M, Guid.NewGuid());

		_ = fakePlayerBucketClient.ReadBucketsAsync(Arg.Is(playerCode))
			.Returns(normalResult);
		_ = fakePlayerBucketClient.ReadBucketsAsync(Arg.Is($"{playerCode}:Game1"))
			.Returns(game1Result);
		_ = fakePlayerBucketClient.ReadBucketsAsync(Arg.Is($"{playerCode}:Game2"))
			.Returns(game2Result);

		var expected = new[]
		{
			new TypeBucketResult(
				null,
				normalResult),
			new TypeBucketResult(
				"Game1",
				game1Result),
			new TypeBucketResult(
				"Game2",
				game2Result)
		};

		// Act
		var actual = await sut.ReadBucketsForBucketTypesAsync(playerCode, gameTypes).ToArrayAsync();

		// Assert
		Assert.Equal(expected, actual);
		_ = fakePlayerBucketClient.Received(3)
			.ReadBucketsAsync(Arg.Any<string>());
	}

	[Fact]
	public async Task 清除所有水桶含遊戲類型()
	{
		// Arrange
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

		var sut = new PlayerBucketForTypesClient(fakePlayerBucketClient);

		var playerCode = "test1";
		var allBucketAnchorIds = new[]
		{
			(BucketType: default(string), AnchorBucketId: Guid.NewGuid()),
			(BucketType: "Game1", AnchorBucketId: Guid.NewGuid()),
			(BucketType: "Game2", AnchorBucketId: Guid.NewGuid())
		};
		// Act
		await sut.CleanBucketsForBucketTypesAsync(playerCode, allBucketAnchorIds, null, null);

		// Assert
		_ = fakePlayerBucketClient.Received(1)
			.CleanBucketsAsync(
				Arg.Is(playerCode),
				Arg.Is(allBucketAnchorIds[0].AnchorBucketId),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(1)
			.CleanBucketsAsync(
				Arg.Is($"{playerCode}:{allBucketAnchorIds[1].BucketType}"),
				Arg.Is(allBucketAnchorIds[1].AnchorBucketId),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(1)
			.CleanBucketsAsync(
				Arg.Is($"{playerCode}:{allBucketAnchorIds[2].BucketType}"),
				Arg.Is(allBucketAnchorIds[2].AnchorBucketId),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
		_ = fakePlayerBucketClient.Received(3)
			.CleanBucketsAsync(
				Arg.Any<string>(),
				Arg.Any<Guid>(),
				Arg.Is(default(string)),
				Arg.Is(default(string)));
	}

	[Fact]
	public async Task 取得玩家遊戲類型水桶列表()
	{
		// Arrange
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

		var sut = new PlayerBucketForTypesClient(fakePlayerBucketClient);

		var playerCode = "test1";
		var gameType = "Game1";

		// Act
		_ = await sut.GetBucketsByPlayerCodeAsync(playerCode, gameType).ToArrayAsync();

		// Assert
		_ = fakePlayerBucketClient.Received(1)
			.GetBucketsByPlayerCodeAsync(Arg.Is($"{playerCode}:{gameType}"));
	}
}
