using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.PlayerBucket.Clients;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Valhalla.MessageQueue;
using Xunit.Abstractions;

namespace PlayerBucketService.IntegrationTests;

public class BrandSettingsApiTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public BrandSettingsApiTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 設定Brand的遊戲水桶種類()
    {
        // Arrange
        await using var web = PlayerBucketServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper)));

        var fakeClientMessageSender = Substitute.For<IMessageSender>();
        var httpClient = web.CreateClient();
        var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

        var dbContext = web.Services.GetRequiredService<TgsContext>();

        var brandId = Guid.NewGuid();
        var bucketTypes = new[] { "Test1", "Test2" };

        // Act
        await sut.SetBrandBucketTypesAsync(brandId, bucketTypes);
        var actual = await dbContext.BrandBucketTypes.FindAsync(brandId);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(bucketTypes, JsonSerializer.Deserialize<string[]>(actual!.BucketTypes));
    }

    [Fact]
    public async Task 取得Brand設定的水桶種類()
    {
        // Arrange
        await using var web = PlayerBucketServiceTestHost.Instance
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper)));

        var fakeClientMessageSender = Substitute.For<IMessageSender>();
        var httpClient = web.CreateClient();
        var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

        var dbContext = web.Services.GetRequiredService<TgsContext>();

        var brandId = Guid.NewGuid();
        var bucketTypes = new[] { "Test1", "Test2" };

        _ = await dbContext.BrandBucketTypes.AddAsync(new BrandBucketType
        {
            BrandId = brandId,
            BucketTypes = JsonSerializer.Serialize(bucketTypes)
        });

        _ = await dbContext.SaveChangesAsync();

        // Act
        var actual = await sut.GetBrandBucketTypesAsync(brandId).ToArrayAsync();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(bucketTypes, actual);
    }
}
