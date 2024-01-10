using NSubstitute;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;

namespace PlayerBucketService.Core.UnitTests;

public class SetBrandBucketTypesCommandServiceTests
{
	[Fact]
	public async Task 設定Brand的水桶類型()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new SetBrandBucketTypesCommandService(fakeTgsRepository);

		var command = new SetBrandBucketTypesCommand(
			Guid.NewGuid(),
			["Test1", "Test2"]);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SetBrandBucketTypesAsync(
				Arg.Is(command.BrandId),
				Arg.Is<IEnumerable<string>>(arg => arg.SequenceEqual(command.BucketTypes)));
	}
}
