using NSubstitute;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;

namespace PlayerBucketService.Core.UnitTests;

public class PlayerBucketTests
{
	[Fact]
	public async Task 取得玩家Bucket記錄()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetPlayerBucketsQueryService(fakeTgsRepository);

		var query = new GetPlayerBucketsQuery("test1");

		// Act
		_ = await sut.QueryAsync(query).ToArrayAsync();

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPlayerBucketsAsync(Arg.Is(query.PlayerCode));
	}

	[Fact]
	public async Task 取得Bucket歷史記錄()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetPlayerBucketHistoriesQueryService(fakeTgsRepository);

		var query = new GetPlayerBucketHistoriesQuery(Guid.NewGuid());

		// Act
		_ = await sut.QueryAsync(query).ToArrayAsync();

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPlayerBucketHistoriesAsync(Arg.Is(query.BucketId));
	}

	[Fact]
	public async Task 依據EventId取得之後的Bucket歷史記錄()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetPlayerBucketHistoriesQueryService(fakeTgsRepository);

		var query = new GetPlayerBucketHistoriesByIdQuery(Guid.NewGuid(), Guid.NewGuid());

		// Act
		_ = await sut.QueryAsync(query).ToArrayAsync();

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPlayerBucketHistoriesByIdAsync(
				Arg.Is(query.BucketId),
				Arg.Is(query.EventId));
	}

	[Fact]
	public async Task 依據QueryDate取得之後的Bucket歷史記錄()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetPlayerBucketHistoriesQueryService(fakeTgsRepository);

		var queryDate = DateTime.Now;
		var query = new GetPlayerBucketHistoriesByDateQuery(Guid.NewGuid(), queryDate);

		// Act
		_ = await sut.QueryAsync(query).ToArrayAsync();

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPlayerBucketHistoriesByDateAsync(
				Arg.Is(query.BucketId),
				Arg.Is(queryDate));
	}
}
