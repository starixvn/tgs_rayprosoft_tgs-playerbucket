using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.MessageQueue;
using Tgs.PlayerBucket.Clients;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Tgs.Service.PlayerBuckets;
using Xunit;
using Xunit.Abstractions;

namespace PlayerBucketService.IntegrationTests;

public class PlayerBucketApiTests
{
	public ITestOutputHelper TestOutputHelper { get; }

	public PlayerBucketApiTests(ITestOutputHelper testOutputHelper)
	{
		TestOutputHelper = testOutputHelper;
	}

	[Fact]
	public async Task 查詢玩家Buckets()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var playerCode = "test1";

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		await dbContext.PlayerBuckets.AddRangeAsync(
			new PlayerBucket
			{
				Id = Guid.NewGuid(),
				BucketAmount = 100M,
				BucketType = (short)BucketType.Deposit,
				CurrentBucketAmount = 0M,
				IsFull = false,
				CreatedOn = DateTime.UtcNow.AddDays(-35D),
				PlayerCode = playerCode
			},
			new PlayerBucket
			{
				Id = Guid.NewGuid(),
				BucketAmount = 100M,
				BucketType = (short)BucketType.Deposit,
				CurrentBucketAmount = 100M,
				IsFull = true,
				CreatedOn = DateTime.UtcNow.AddDays(-3D),
				PlayerCode = playerCode
			},
			new PlayerBucket
			{
				Id = Guid.NewGuid(),
				BucketAmount = 100M,
				BucketType = (short)BucketType.Deposit,
				CurrentBucketAmount = 100M,
				IsFull = true,
				CreatedOn = DateTime.UtcNow.AddDays(-31D),
				PlayerCode = playerCode
			});

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.GetBucketsByPlayerCodeAsync(playerCode).ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(2, actual.Length);
	}

	[Fact]
	public async Task 查詢Bucket的歷程()
	{
		/// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var bucketId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		await dbContext.PlayerBucketEventSources.AddRangeAsync(
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097c77de-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = 100M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Deposit),
				CreatedOn = DateTime.UtcNow
			},
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097c8e45-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = -10M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Game),
				CreatedOn = DateTime.UtcNow
			},
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097cd6a5-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = -10M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Game),
				CreatedOn = DateTime.UtcNow
			});
		;

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.GetBucketEventsAsync(bucketId).ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(3, actual.Length);
	}
}
