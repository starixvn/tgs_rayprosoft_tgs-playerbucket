using System.Text.Json;
using ExpectedObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
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
		// Arrange
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

	[Fact]
	public async Task 查詢Bucket的歷程_依據EventId查詢條件()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var bucketId = Guid.NewGuid();
		var eventId = Guid.Parse("097c77de-8401-0000-0001-000080cd2019");

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		await dbContext.PlayerBucketEventSources.AddRangeAsync(
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097c77de-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = 100M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Deposit),
				CreatedOn = DateTime.Parse("2023-01-03 12:00:00Z")
			},
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097c8e45-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = -10M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Game),
				CreatedOn = DateTime.Parse("2023-01-04 12:00:00Z")
			},
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097cd6a5-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = -10M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Game),
				CreatedOn = DateTime.Parse("2023-01-05 12:00:00Z")
			});

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.GetBucketEventsAsync(bucketId, eventId).ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(2, actual.Length);
	}

	[Fact]
	public async Task 查詢Bucket的歷程_依據QueryDate查詢條件()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var bucketId = Guid.NewGuid();
		var queryDate = DateTime.Parse("2023-01-04 12:00:00Z");

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		await dbContext.PlayerBucketEventSources.AddRangeAsync(
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097c77de-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = 100M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Deposit),
				CreatedOn = DateTime.Parse("2023-01-03 12:00:00Z")
			},
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097c8e45-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = -10M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Game),
				CreatedOn = DateTime.Parse("2023-01-04 12:00:00Z")
			},
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097cd6a5-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = -10M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Game),
				CreatedOn = DateTime.Parse("2023-01-05 12:00:00Z")
			});

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.GetBucketEventsAsync(bucketId, queryDate).ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		_ = Assert.Single(actual);
	}

	[Fact]
	public async Task 查詢玩家Buckets_比對資料()
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

		var bucketId = Guid.NewGuid();
		var createdOn = DateTime.UtcNow.AddDays(-35D);

		await dbContext.PlayerBuckets.AddRangeAsync(
			new PlayerBucket
			{
				Id = bucketId,
				BucketAmount = 100M,
				BucketType = (short)BucketType.Deposit,
				CurrentBucketAmount = 0M,
				IsFull = false,
				CreatedOn = createdOn,
				PlayerCode = playerCode
			});

		_ = await dbContext.SaveChangesAsync();

		var expected = new BucketInfo(
			bucketId,
			playerCode,
			100M,
			BucketType.Deposit,
			0M,
			false,
			createdOn,
			null);

		// Act
		var actual = await sut.GetBucketsByPlayerCodeAsync(playerCode).ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(expected, actual.FirstOrDefault());
	}

	[Fact]
	public async Task 查詢Bucket的歷程_比對資料()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var bucketId = Guid.NewGuid();
		var eventId = Guid.Parse("097c77de-8401-0000-0001-000080cd2019");
		var createdOn = DateTime.UtcNow;

		await dbContext.PlayerBucketEventSources.AddRangeAsync(
			new PlayerBucketEventSource
			{
				Id = Guid.Parse("097c77de-8401-0000-0001-000080cd2019"),
				BucketId = bucketId,
				ChangeAmount = 100M,
				ChangeSets = "[]",
				FillType = nameof(FillType.Deposit),
				CreatedOn = createdOn,
				Remark = "remark",
				ExternalId = "external"
			});

		_ = await dbContext.SaveChangesAsync();

		var expected = new BucketEvent(
			eventId,
			bucketId,
			100M,
			FillType.Deposit,
			"external",
			JsonDocument.Parse("[]"),
			createdOn,
			"remark");

		// Act
		var actual = await sut.GetBucketEventsAsync(bucketId).ToArrayAsync();

		// Assert
		Assert.NotNull(actual);
		expected.ToExpectedObject(context =>
		{
			_ = context.IgnoreEqualsOverride();
			_ = context.Ignore(evt => evt.ChangeSets);
		}).ShouldMatch(actual.FirstOrDefault());
	}

	[Fact]
	public async Task 啟動清除流水流程()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var playerCode = "test1";
		var cleanOrder = new CleanBucketOrder(
			Guid.NewGuid(),
			0M,
			1000M,
			Guid.NewGuid(),
			Guid.NewGuid());

		var expected = new
		{
			PlayerCode = playerCode,
			cleanOrder.BrandId,
			cleanOrder.CurrentBalance,
			cleanOrder.CurrentBucketAmount,
			cleanOrder.BucketAnchorId,
			cleanOrder.DepositFormId
		};

		// Act
		await sut.RequestCleanBucketsAsync(playerCode, cleanOrder);

		var row = await dbContext.PlayerBucketCleanForms.SingleAsync();

		// Assert
		expected.ToExpectedObject().ShouldMatch(row);
	}

	[Fact]
	public async Task 取得清除流水表單內容()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row = new PlayerBucketCleanForm
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		_ = await dbContext.PlayerBucketCleanForms.AddAsync(row);

		_ = await dbContext.SaveChangesAsync();

		var expected = new CleanBucketForm(
			row.Id,
			row.PlayerCode,
			row.BrandId,
			row.CurrentBalance,
			row.CurrentBucketAmount,
			row.BucketAnchorId,
			row.DepositFormId,
			CleanFlowStatus.Create,
			row.CreatedOn);

		// Act
		var actual = await sut.GetCleanBucketFormAsync(row.Id);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task 取得清除流水表單內容但是資料庫沒有資料時傳回Null()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var formId = Guid.NewGuid();

		// Act
		var actual = await sut.GetCleanBucketFormAsync(formId);

		// Assert
		Assert.Null(actual);
	}
}
