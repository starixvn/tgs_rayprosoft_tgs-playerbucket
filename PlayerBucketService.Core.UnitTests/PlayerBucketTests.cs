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
}
