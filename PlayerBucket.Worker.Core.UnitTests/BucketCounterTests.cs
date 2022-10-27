using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.BucketCommands;

namespace PlayerBucket.Worker.Core.UnitTests;

[TestClass]
public class BucketCounterTests
{
	[TestMethod]
	public async Task BucketCounter_PushCommand會被執行()
	{
		var sut = new BucketCounter(
			"test1",
			Substitute.For<ISerialNumberGenerator>(),
			Substitute.For<ITgsRepository>(),
			Substitute.For<IBucketEventRepository>(),
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var command = new StubBucketCommand();

		await sut.PrepareAsync();
		await sut.PushCommandAsync(command);

		await command.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.IsTrue(command.IsExcuted);
	}

	[TestMethod]
	public async Task BucketCounter_執行建立Bucket()
	{
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new BucketCounter(
			"test1",
			fakeSerialNumberGenerator,
			fakeTgsRepository,
			fakeBucketEventRepository,
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var externalId = Guid.NewGuid().ToString();
		var command = new CreateBucketCommand(100M, BucketType.Deposit, externalId);

		var expected = new BucketCreateInfo("test1", 100M, BucketType.Deposit, externalId);

		await sut.PrepareAsync();
		await sut.PushCommandAsync(command);

		_ = await command.GetCreatedIdAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeBucketEventRepository.Received(1)
			.AddPlayerBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(expected),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task BucketCounter_執行填充Bucket()
	{
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new BucketCounter(
			"test1",
			fakeSerialNumberGenerator,
			fakeTgsRepository,
			fakeBucketEventRepository,
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var externalId = Guid.NewGuid().ToString();
		var command = new FillInBucketCommand(100M, FillType.Game, externalId);

		var expected = new FillInBucketInfo("test1", 100M, FillType.Game, externalId, false);

		_ = fakeTgsRepository.GetPlayerNotFullBucketsAsync(
			Arg.Is("test1"),
			Arg.Any<CancellationToken>())
			.Returns(new[] {
				new BucketInfo(
					Guid.NewGuid(),
					"test1",
					1000M,
					BucketType.Deposit,
					0M,
					false,
					null)
			}.ToAsyncEnumerable());

		await sut.PrepareAsync();
		await sut.PushCommandAsync(command);

		await command.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeBucketEventRepository.Received(1)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(expected),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task BucketCounter_填充Bucket_填充到滿後就不會再填充()
	{
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new BucketCounter(
			"test1",
			fakeSerialNumberGenerator,
			fakeTgsRepository,
			fakeBucketEventRepository,
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var externalId = Guid.NewGuid().ToString();
		var command1 = new FillInBucketCommand(10M, FillType.Game, externalId);
		var command2 = new FillInBucketCommand(10M, FillType.Game, externalId);
		var command3 = new FillInBucketCommand(10M, FillType.Game, externalId);
		var command4 = new FillInBucketCommand(10M, FillType.Game, externalId);

		var expected = new FillInBucketInfo("test1", 10M, FillType.Game, externalId, false);

		_ = fakeTgsRepository.GetPlayerNotFullBucketsAsync(
			Arg.Is("test1"),
			Arg.Any<CancellationToken>())
			.Returns(new[] {
				new BucketInfo(
					Guid.NewGuid(),
					"test1",
					30M,
					BucketType.Deposit,
					0M,
					false,
					null)
			}.ToAsyncEnumerable());

		await sut.PrepareAsync();
		await sut.PushCommandAsync(command1);
		await sut.PushCommandAsync(command2);
		await sut.PushCommandAsync(command3);
		await sut.PushCommandAsync(command4);

		await command1.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));
		await command2.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));
		await command3.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));
		await command4.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeBucketEventRepository.Received(2)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(expected),
				Arg.Any<CancellationToken>());
		_ = fakeBucketEventRepository.Received(1)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(expected with { SetFull = true }),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task BucketCounter_建立Bucket後_可以填充Bucket到滿()
	{
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new BucketCounter(
			"test1",
			fakeSerialNumberGenerator,
			fakeTgsRepository,
			fakeBucketEventRepository,
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var externalId = Guid.NewGuid().ToString();
		var createCommand = new CreateBucketCommand(30M, BucketType.Deposit, externalId);
		var command1 = new FillInBucketCommand(10M, FillType.Game, externalId);
		var command2 = new FillInBucketCommand(10M, FillType.Game, externalId);
		var command3 = new FillInBucketCommand(10M, FillType.Game, externalId);
		var command4 = new FillInBucketCommand(10M, FillType.Game, externalId);

		var createExpected = new BucketCreateInfo("test1", 30M, BucketType.Deposit, externalId);
		var fillExpected = new FillInBucketInfo("test1", 10M, FillType.Game, externalId, false);

		await sut.PrepareAsync();
		await sut.PushCommandAsync(createCommand);
		await sut.PushCommandAsync(command1);
		await sut.PushCommandAsync(command2);
		await sut.PushCommandAsync(command3);
		await sut.PushCommandAsync(command4);

		_ = await createCommand.GetCreatedIdAsync().WaitAsync(TimeSpan.FromSeconds(1D));
		await command1.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));
		await command2.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));
		await command3.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));
		await command4.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeBucketEventRepository.Received(1)
			.AddPlayerBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(createExpected),
				Arg.Any<CancellationToken>());
		_ = fakeBucketEventRepository.Received(2)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(fillExpected),
				Arg.Any<CancellationToken>());
		_ = fakeBucketEventRepository.Received(1)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(fillExpected with { SetFull = true }),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task BucketCounter_Prepare兩次不會出錯()
	{
		var sut = new BucketCounter(
			"test1",
			Substitute.For<ISerialNumberGenerator>(),
			Substitute.For<ITgsRepository>(),
			Substitute.For<IBucketEventRepository>(),
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var command = new StubBucketCommand();

		await sut.PrepareAsync();
		await sut.PrepareAsync();
		await sut.PushCommandAsync(command);

		await command.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.IsTrue(command.IsExcuted);
	}

	[TestMethod]
	public async Task BucketCounter_建立Bucket後_取得玩家的洗碼量()
	{
		var sut = new BucketCounter(
			"test1",
			Substitute.For<ISerialNumberGenerator>(),
			Substitute.For<ITgsRepository>(),
			Substitute.For<IBucketEventRepository>(),
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		await sut.PrepareAsync();

		var createCommand = new CreateBucketCommand(30M, BucketType.Deposit, Guid.NewGuid().ToString());

		await sut.PushCommandAsync(createCommand);

		var getCommand = new ReadBucketAmountCommand();

		await sut.PushCommandAsync(getCommand);

		var actual = await getCommand.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.AreEqual(30M, actual.Amount);
	}

	[TestMethod]
	public async Task BucketCounter_FillIn後_取得玩家洗碼量()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new BucketCounter(
			"test1",
			Substitute.For<ISerialNumberGenerator>(),
			fakeTgsRepository,
			Substitute.For<IBucketEventRepository>(),
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		_ = fakeTgsRepository.GetPlayerNotFullBucketsAsync(
			Arg.Is("test1"),
			Arg.Any<CancellationToken>())
			.Returns(new[] {
				new BucketInfo(
					Guid.NewGuid(),
					"test1",
					30M,
					BucketType.Deposit,
					0M,
					false,
					null)
			}.ToAsyncEnumerable());

		await sut.PrepareAsync();

		var fillCommand = new FillInBucketCommand(10M, FillType.Game, Guid.NewGuid().ToString());

		await sut.PushCommandAsync(fillCommand);

		var getCommand = new ReadBucketAmountCommand();

		await sut.PushCommandAsync(getCommand);

		var actual = await getCommand.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.AreEqual(20M, actual.Amount);
	}

	[TestMethod]
	public async Task BucketCounter_Prepare後取得玩家洗碼量是一個概算值_當FillIn的時候會計算當下Bucket的實際值()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		var sut = new BucketCounter(
			"test1",
			Substitute.For<ISerialNumberGenerator>(),
			fakeTgsRepository,
			fakeBucketEventRepository,
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var buckets = new[] {
			new BucketInfo(
				Guid.Parse("097cdb7f-8401-0000-0001-000080cd2019"),
				"test1",
				10M,
				BucketType.Deposit,
				0M,
				false,
				null),
			new BucketInfo(
				Guid.Parse("098412ec-8401-0000-0001-000080cd2019"),
				"test1",
				100M,
				BucketType.Deposit,
				0M,
				false,
				Guid.Parse("097c77de-8401-0000-0001-000080cd2019"))
		};

		_ = fakeTgsRepository.GetHasMigrateAsync(
			Arg.Is("test1"),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakeTgsRepository.GetPlayerNotFullBucketsAsync(
			Arg.Is("test1"),
			Arg.Any<CancellationToken>())
			.Returns(buckets.ToAsyncEnumerable());

		_ = fakeBucketEventRepository.GetBucketAllEventsAsync(
			Arg.Is(buckets[1].Id),
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new[] {
				new BucketEvent(
					Guid.Parse("097c8e45-8401-0000-0001-000080cd2019"),
					buckets[1].Id,
					-10M,
					FillType.Game,
					null,
					JsonDocument.Parse("[]"),
					DateTime.UtcNow),
				new BucketEvent(
					Guid.Parse("097cd6a5-8401-0000-0001-000080cd2019"),
					buckets[1].Id,
					-20M,
					FillType.Game,
					null,
					JsonDocument.Parse("[]"),
					DateTime.UtcNow)
			}.ToAsyncEnumerable());

		await sut.PrepareAsync();

		var firstGetCommand = new ReadBucketAmountCommand();

		await sut.PushCommandAsync(firstGetCommand);

		var firstAmountResult = await firstGetCommand.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		var fillCommand = new FillInBucketCommand(10M, FillType.Game, Guid.NewGuid().ToString());

		await sut.PushCommandAsync(fillCommand);

		var secondGetCommand = new ReadBucketAmountCommand();

		await sut.PushCommandAsync(secondGetCommand);

		var secondAmountResult = await secondGetCommand.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		await sut.PushCommandAsync(new FillInBucketCommand(10M, FillType.Game, Guid.NewGuid().ToString()));

		var getCommand = new ReadBucketAmountCommand();

		await sut.PushCommandAsync(getCommand);

		var actual = await getCommand.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.AreEqual(110M, firstAmountResult.Amount);
		Assert.AreEqual(100M, secondAmountResult.Amount);
		Assert.AreEqual(60M, actual.Amount);
	}

	[TestMethod]
	public async Task BucketCounter_清除玩家Buckets()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new BucketCounter(
			"test1",
			Substitute.For<ISerialNumberGenerator>(),
			fakeTgsRepository,
			Substitute.For<IBucketEventRepository>(),
			Substitute.For<IEventSource>(),
			NullLogger<BucketCounter>.Instance);

		var buckets = new Queue<BucketInfo>(new[]
		{
			new BucketInfo(
				Guid.Parse("0985a64a-8401-0000-0001-000080cd2019"),
				"test1",
				100M,
				BucketType.Deposit,
				10M,
				false,
				null),
			new BucketInfo(
				Guid.Parse("0987b455-8401-0000-0001-000080cd2019"),
				"test1",
				100M,
				BucketType.Deposit,
				30M,
				false,
				null),
			new BucketInfo(
				Guid.Parse("098bb7eb-8401-0000-0001-000080cd2019"),
				"test1",
				100M,
				BucketType.Deposit,
				40M,
				false,
				null)
		});

		_ = fakeTgsRepository.GetPlayerNotFullBucketsAsync(
			Arg.Is("test1"),
			Arg.Any<CancellationToken>())
			.Returns(buckets.ToAsyncEnumerable());

		_ = fakeTgsRepository.GetHasMigrateAsync(Arg.Is("test1"), Arg.Any<CancellationToken>())
			.Returns(true);

		var anchorId = Guid.Parse("0987b455-8401-0000-0001-000080cd2019");

		await sut.PrepareAsync();

		var firstGetCommand = new ReadBucketAmountCommand();

		await sut.PushCommandAsync(firstGetCommand);

		var initAmountResult = await firstGetCommand.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		var cleanCommand = new CleanBucketsCommand(anchorId, Guid.NewGuid().ToString());

		await sut.PushCommandAsync(cleanCommand);

		var summaryGetCommand = new ReadBucketAmountCommand();

		await sut.PushCommandAsync(summaryGetCommand);

		var finalAmountResult = await summaryGetCommand.GetAmountAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.AreEqual(220M, initAmountResult.Amount);
		Assert.AreEqual(60M, finalAmountResult.Amount);
	}
}
