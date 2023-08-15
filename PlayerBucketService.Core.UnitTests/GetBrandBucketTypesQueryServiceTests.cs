using NSubstitute;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;

namespace PlayerBucketService.Core.UnitTests;

public class GetBrandBucketTypesQueryServiceTests
{
	[Fact]
	public async Task 取得Brand的BucketTypes設定()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetBrandBucketTypesQueryService(fakeTgsRepository);

		var query = new GetBrandBucketTypesQuery(Guid.NewGuid());

		// Act
		_ = await sut.QueryAsync(query).ToArrayAsync();

		// Assert
		_ = fakeTgsRepository.Received(1)
			.GetBrandBucketTypesAsync(Arg.Is(query.BrandId));
	}
}
