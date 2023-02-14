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
			Guid.NewGuid(),
			0M,
			1000M,
			Guid.NewGuid(),
			Guid.NewGuid(),
			5M);

		var expected = new
		{
			PlayerCode = playerCode,
			cleanOrder.BrandId,
			cleanOrder.WalletId,
			cleanOrder.CurrentBalance,
			cleanOrder.CurrentBucketAmount,
			cleanOrder.BucketAnchorId,
			cleanOrder.DepositFormId,
			cleanOrder.ClearWageringRequirementBalance
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
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			ClearWageringRequirementBalance = 5M,
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		_ = await dbContext.PlayerBucketCleanForms.AddAsync(row);

		_ = await dbContext.SaveChangesAsync();

		var expected = new CleanBucketForm(
			row.Id,
			row.PlayerCode,
			row.WalletId,
			row.BrandId,
			row.CurrentBalance,
			row.CurrentBucketAmount,
			row.BucketAnchorId,
			row.DepositFormId,
			row.ClearWageringRequirementBalance,
			null,
			null,
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

	[Fact]
	public async Task 完成清除流水表單()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var formId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		_ = await dbContext.PlayerBucketCleanForms.AddAsync(row);

		_ = await dbContext.SaveChangesAsync();

		var request = new ReviewCleanBucketRequest<object>(100M, 2000M, new object());

		var expected = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = row.BrandId,
			WalletId = row.WalletId,
			BucketAnchorId = row.BucketAnchorId,
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = row.ClearWageringRequirementBalance,
			CleaningBalance = request.CleaningBalance,
			CleaningBucketAmount = request.CleaningBucketAmount,
			GameBalanceReturn = "{}",
			DepositFormId = row.DepositFormId,
			Status = CleanFlowStatus.Completed.ToString(),
			CreatedOn = row.CreatedOn
		};

		// Act
		await sut.CompleteCleanBucketsAsync(formId, request);

		// Assert
		expected.ToExpectedObject().ShouldMatch(row);
	}

	[Fact]
	public async Task 完成清除流水表單_如果GameBalanceReturn為空_資料表欄位會是Null()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var formId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		_ = await dbContext.PlayerBucketCleanForms.AddAsync(row);

		_ = await dbContext.SaveChangesAsync();

		var request = new ReviewCleanBucketRequest<object?>(100M, 2000M, null);

		var expected = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = row.BrandId,
			WalletId = row.WalletId,
			BucketAnchorId = row.BucketAnchorId,
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = row.ClearWageringRequirementBalance,
			CleaningBalance = request.CleaningBalance,
			CleaningBucketAmount = request.CleaningBucketAmount,
			GameBalanceReturn = null,
			DepositFormId = row.DepositFormId,
			Status = CleanFlowStatus.Completed.ToString(),
			CreatedOn = row.CreatedOn
		};

		// Act
		await sut.CompleteCleanBucketsAsync(formId, request);

		// Assert
		expected.ToExpectedObject().ShouldMatch(row);
	}

	[Fact]
	public async Task 拒絕清除流水表單()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var formId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		_ = await dbContext.PlayerBucketCleanForms.AddAsync(row);

		_ = await dbContext.SaveChangesAsync();

		var request = new ReviewCleanBucketRequest<object>(100M, 2000M, new object());

		var expected = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = row.BrandId,
			WalletId = row.WalletId,
			BucketAnchorId = row.BucketAnchorId,
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = row.ClearWageringRequirementBalance,
			CleaningBalance = request.CleaningBalance,
			CleaningBucketAmount = request.CleaningBucketAmount,
			GameBalanceReturn = "{}",
			DepositFormId = row.DepositFormId,
			Status = CleanFlowStatus.Rejected.ToString(),
			CreatedOn = row.CreatedOn
		};

		// Act
		await sut.RejectCleanBucketsAsync(formId, request);

		// Assert
		expected.ToExpectedObject().ShouldMatch(row);
	}

	[Fact]
	public async Task 拒絕清除流水表單_如果GameBalanceReturn為空_資料庫欄位會是Null()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var formId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		_ = await dbContext.PlayerBucketCleanForms.AddAsync(row);

		_ = await dbContext.SaveChangesAsync();

		var request = new ReviewCleanBucketRequest<object?>(100M, 2000M, null);

		var expected = new PlayerBucketCleanForm
		{
			Id = formId,
			BrandId = row.BrandId,
			WalletId = row.WalletId,
			BucketAnchorId = row.BucketAnchorId,
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = row.ClearWageringRequirementBalance,
			CleaningBalance = request.CleaningBalance,
			CleaningBucketAmount = request.CleaningBucketAmount,
			GameBalanceReturn = null,
			DepositFormId = row.DepositFormId,
			Status = CleanFlowStatus.Rejected.ToString(),
			CreatedOn = row.CreatedOn
		};

		// Act
		await sut.RejectCleanBucketsAsync(formId, request);

		// Assert
		expected.ToExpectedObject().ShouldMatch(row);
	}

	[Fact]
	public async Task 取得清流水表單列表()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var playerCode = "test1";
		var form1Id = Guid.NewGuid();
		var form2Id = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row1 = new PlayerBucketCleanForm
		{
			Id = form1Id,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow.AddDays(-1D)
		};

		var row2 = new PlayerBucketCleanForm
		{
			Id = form2Id,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		await dbContext.PlayerBucketCleanForms.AddRangeAsync(row1, row2);

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.QueryCleanBucketFomsAsync(playerCode).ToArrayAsync();

		// Assert
		Assert.Equal(2, actual.Length);
		Assert.Equal(new[] { form2Id, form1Id }, actual.Select(form => form.Id));
	}

	[Fact]
	public async Task 取得清流水表單列表只會列出30天以內的資料()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var playerCode = "test1";
		var form1Id = Guid.NewGuid();
		var form2Id = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row1 = new PlayerBucketCleanForm
		{
			Id = form1Id,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow.AddDays(-30D)
		};

		var row2 = new PlayerBucketCleanForm
		{
			Id = form2Id,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		await dbContext.PlayerBucketCleanForms.AddRangeAsync(row1, row2);

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.QueryCleanBucketFomsAsync(playerCode).ToArrayAsync();

		// Assert
		_ = Assert.Single(actual);
		Assert.Equal(new[] { form2Id }, actual.Select(form => form.Id));
	}

	[Fact]
	public async Task 取得清流水表單列表最多只會顯示20筆資料()
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

		static IEnumerable<PlayerBucketCleanForm> GenerateForms(int count)
		{
			var generateCount = count;
			while (generateCount-- > 0)
			{
				yield return new PlayerBucketCleanForm
				{
					Id = Guid.NewGuid(),
					BrandId = Guid.NewGuid(),
					WalletId = Guid.NewGuid(),
					BucketAnchorId = Guid.NewGuid(),
					PlayerCode = "test1",
					CurrentBalance = 0M,
					CurrentBucketAmount = 1000M,
					ClearWageringRequirementBalance = 5M,
					CleaningBalance = null,
					CleaningBucketAmount = null,
					DepositFormId = Guid.NewGuid(),
					Status = CleanFlowStatus.Create.ToString(),
					CreatedOn = DateTime.UtcNow
				};
			}
		}

		await dbContext.PlayerBucketCleanForms.AddRangeAsync(GenerateForms(21));

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.QueryCleanBucketFomsAsync(playerCode).ToArrayAsync();

		// Assert
		Assert.Equal(20, actual.Length);
	}

	[Fact]
	public async Task 取得清流水表單列表可以依照CleanFlowStatus過濾資料()
	{
		// Arrange
		await using var web = PlayerBucketServiceTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(TestOutputHelper)));

		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var httpClient = web.CreateClient();
		var sut = new PlayerBucketClient(httpClient, fakeClientMessageSender);

		var playerCode = "test1";
		var form1Id = Guid.NewGuid();
		var form2Id = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var row1 = new PlayerBucketCleanForm
		{
			Id = form1Id,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = null,
			CleaningBucketAmount = null,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Create.ToString(),
			CreatedOn = DateTime.UtcNow.AddDays(-30D)
		};

		var row2 = new PlayerBucketCleanForm
		{
			Id = form2Id,
			BrandId = Guid.NewGuid(),
			WalletId = Guid.NewGuid(),
			BucketAnchorId = Guid.NewGuid(),
			PlayerCode = "test1",
			CurrentBalance = 0M,
			CurrentBucketAmount = 1000M,
			ClearWageringRequirementBalance = 5M,
			CleaningBalance = 0M,
			CleaningBucketAmount = 1000M,
			DepositFormId = Guid.NewGuid(),
			Status = CleanFlowStatus.Completed.ToString(),
			CreatedOn = DateTime.UtcNow
		};

		await dbContext.PlayerBucketCleanForms.AddRangeAsync(row1, row2);

		_ = await dbContext.SaveChangesAsync();

		// Act
		var actual = await sut.QueryCleanBucketFomsAsync(playerCode, CleanFlowStatus.Completed).ToArrayAsync();

		// Assert
		_ = Assert.Single(actual);
		Assert.Equal(new[] { form2Id }, actual.Select(form => form.Id));
	}
}
