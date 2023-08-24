using ExpectedObjects;
using NSubstitute;
using Tgs.SerialNumberGenerator;
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

	[Fact]
	public async Task 建立清除流水申請單()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

		var sut = new ApplyCleanBucketsFormCommandService(
			fakeTgsRepository,
			fakeSerialNumberGenerator,
			fakePlayerBucketClient);

		var command = new ApplyCleanBucketsFormCommand(
			"test1",
			Guid.NewGuid(),
			Guid.NewGuid(),
			0M,
			new[]
			{
				new CleanFormBucketResult(
					null,
					  new ReadBucketResult(
						  1000M,
						  Guid.NewGuid()))
			},
			Guid.NewGuid(),
			0M);

		var createId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync()
			.Returns(createId);

		var expected = new
		{
			Id = createId,
			command.PlayerCode,
			command.BrandId,
			command.WalletId,
			command.CurrentBalance,
			command.CurrentBucketAmounts,
			command.DepositFormId,
			command.ClearWageringRequirementBalance
		};

		// Act
		_ = await sut.GetAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.UpsertCleanPlayerBucketFormAsync(
				Arg.Is<CleanPlayerBucketForm>(form => expected.ToExpectedObject().Matches(form)),
				Arg.Is(default(CleanPlayerBucketForm)));
		_ = fakePlayerBucketClient.Received(1)
			.PublishCleanBucketsFormCreatedEventAsync(Arg.Is(createId));
	}

	[Fact]
	public async Task 取得清除流水申請單內容()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetCleanPlayerBucketsFormQueryService(fakeTgsRepository);

		var query = new GetCleanPlayerBucketsFormQuery(Guid.NewGuid());

		// Act
		_ = await sut.GetAsync(query);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.GetCleanPlayerBucketFormV2Async(Arg.Is(query.FormId));
	}

	[Fact]
	public async Task 取得清除流水申請單內容第二版()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetCleanPlayerBucketsFormQueryService(fakeTgsRepository);

		var query = new GetCleanPlayerBucketsFormV2Query(Guid.NewGuid());

		// Act
		_ = await sut.GetAsync(query);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.GetCleanPlayerBucketFormV2Async(Arg.Is(query.FormId));
	}

	[Fact]
	public async Task 完成清除流水申請單()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new CompleteCleanBucketsFormCommandService(
			fakeTgsRepository);

		var command = new CompleteCleanBucketsFormCommand(
			Guid.NewGuid(),
			100M,
			new[] {
				new CleanFormBucketResult(
					null,
					  new ReadBucketResult(
						  2000M,
						  Guid.NewGuid()))
			},
			null);

		var form = new CleanPlayerBucketForm(
			command.FormId,
			"test1",
			Guid.NewGuid(),
			Guid.NewGuid(),
			0M,
			new[]
			{
				new CleanFormBucketResult(
					null,
					new ReadBucketResult(1000M, Guid.NewGuid()))
			},
			Guid.NewGuid(),
			0M,
			null,
			null,
			null,
			CleanFlowStatus.Create,
			DateTime.UtcNow);

		_ = fakeTgsRepository.GetCleanPlayerBucketFormV2Async(Arg.Is(command.FormId))
			.Returns(form);

		var expectedForm = (form with
		{
			Status = CleanFlowStatus.Completed,
			CleaningBalance = command.CleaningBalance,
			CleaningBucketAmounts = command.CleaningBucketAmounts,
		}).ToExpectedObject(ctx => ctx.IgnoreEqualsOverride());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.UpsertCleanPlayerBucketFormAsync(
				Arg.Is<CleanPlayerBucketForm>(argForm => expectedForm.Matches(argForm)),
				Arg.Is(form));
	}

	[Fact]
	public async Task 否決清除流水申請單()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new RejectCleanBucketsFormCommandService(fakeTgsRepository);

		var command = new RejectCleanBucketsFormCommand(
			Guid.NewGuid(),
			100M,
			new[]
			{
				new CleanFormBucketResult(
					null,
					new ReadBucketResult(
						2000M,
						Guid.NewGuid()))
			},
			null);

		var form = new CleanPlayerBucketForm(
			command.FormId,
			"test1",
			Guid.NewGuid(),
			Guid.NewGuid(),
			0M,
			new[]
			{
				new CleanFormBucketResult(
					null,
					  new ReadBucketResult(1000M, Guid.NewGuid()))
			},
			Guid.NewGuid(),
			0M,
			null,
			null,
			null,
			CleanFlowStatus.Create,
			DateTime.UtcNow);

		_ = fakeTgsRepository.GetCleanPlayerBucketFormV2Async(Arg.Is(command.FormId))
			.Returns(form);

		var expectedForm = (form with
		{
			Status = CleanFlowStatus.Rejected,
			CleaningBalance = command.CleaningBalance,
			CleaningBucketAmounts = command.CleaningBucketAmounts,
		}).ToExpectedObject(ctx => ctx.IgnoreEqualsOverride());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.UpsertCleanPlayerBucketFormAsync(
				Arg.Is<CleanPlayerBucketForm>(argForm => expectedForm.Matches(argForm)),
				Arg.Is(form));
	}

	[Fact]
	public async Task 依照PlayerCode查詢清除流水申請單列表()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetCleanBucketsFormListQueryService(fakeTgsRepository);

		var query = new GetCleanBucketsFormListQuery("test1", null);

		// Act
		_ = await sut.QueryAsync(query).ToArrayAsync();

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryCleanPlayerBucketFormsV2Async(
				Arg.Is(query.PlayerCode),
				Arg.Is(default(CleanFlowStatus?)));
	}

	[Fact]
	public async Task 依照PlayerCode查詢清除流水申請單列表第二版()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetCleanBucketsFormListQueryService(fakeTgsRepository);

		var query = new GetCleanBucketsFormListV2Query("test1", null);

		// Act
		_ = await sut.QueryAsync(query).ToArrayAsync();

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryCleanPlayerBucketFormsV2Async(
				Arg.Is(query.PlayerCode),
				Arg.Is(default(CleanFlowStatus?)));
	}
}
