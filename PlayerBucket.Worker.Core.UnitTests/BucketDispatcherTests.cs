using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Worker.Core.UnitTests;

[TestClass]
public class BucketDispatcherTests
{
	[TestMethod]
	public async Task BucketDispatcher_分派Command()
	{
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new BucketDispatcher(fakeServiceProvider);

		var command = new StubBucketCommand();

		var fakeServiceScopeFactory = Substitute.For<IServiceScopeFactory>();
		_ = fakeServiceProvider.GetService<IServiceScopeFactory>()
			.Returns(fakeServiceScopeFactory);

		var fakeScope = Substitute.For<IServiceScope>();
		_ = fakeScope.ServiceProvider
			.Returns(fakeServiceProvider);
		_ = fakeServiceScopeFactory.CreateScope()
			.Returns(fakeScope);

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService<ISerialNumberGenerator>()
			.Returns(fakeSerialNumberGenerator);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		_ = fakeServiceProvider.GetService<ITgsRepository>()
			.Returns(fakeTgsRepository);

		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();
		_ = fakeServiceProvider.GetService<IBucketEventRepository>()
			.Returns(fakeBucketEventRepository);

		var fakeEventSource = Substitute.For<IEventSource>();
		_ = fakeServiceProvider.GetService<IEventSource>()
			.Returns(fakeEventSource);

		_ = fakeServiceProvider.GetService<ILogger<BucketCounter>>()
			.Returns(NullLogger<BucketCounter>.Instance);

		await sut.PushCommandAsync("test1", command);

		await command.WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.IsTrue(command.IsExcuted);
	}

	[TestMethod]
	public async Task BucketDispatcher_test1_建立30的Bucket_填入四次10_應該只填三次()
	{
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new BucketDispatcher(fakeServiceProvider);

		var fakeServiceScopeFactory = Substitute.For<IServiceScopeFactory>();
		_ = fakeServiceProvider.GetService<IServiceScopeFactory>()
			.Returns(fakeServiceScopeFactory);

		var fakeScope = Substitute.For<IServiceScope>();
		_ = fakeScope.ServiceProvider
			.Returns(fakeServiceProvider);
		_ = fakeServiceScopeFactory.CreateScope()
			.Returns(fakeScope);

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService<ISerialNumberGenerator>()
			.Returns(fakeSerialNumberGenerator);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		_ = fakeServiceProvider.GetService<ITgsRepository>()
			.Returns(fakeTgsRepository);

		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();
		_ = fakeServiceProvider.GetService<IBucketEventRepository>()
			.Returns(fakeBucketEventRepository);

		var fakeEventSource = Substitute.For<IEventSource>();
		_ = fakeServiceProvider.GetService<IEventSource>()
			.Returns(fakeEventSource);

		_ = fakeServiceProvider.GetService<ILogger<BucketCounter>>()
			.Returns(NullLogger<BucketCounter>.Instance);

		var createCommand = new CreateBucketCommand(30M, BucketType.Deposit, null);

		var command1 = new FillInBucketCommand(10M, FillType.Game, null);
		var command2 = new FillInBucketCommand(10M, FillType.Game, null);
		var command3 = new FillInBucketCommand(10M, FillType.Game, null);
		var command4 = new FillInBucketCommand(10M, FillType.Game, null);

		var createExpected = new BucketCreateInfo("test1", 30M, BucketType.Deposit, null);
		var fillExpected = new FillInBucketInfo("test1", 10M, FillType.Game, null, false);

		await sut.PushCommandAsync("test1", createCommand);
		await sut.PushCommandAsync("test1", command1);
		await sut.PushCommandAsync("test1", command2);
		await sut.PushCommandAsync("test1", command3);
		await sut.PushCommandAsync("test1", command4);

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
}
