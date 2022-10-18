using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		fakeServiceProvider.GetService<IServiceScopeFactory>()
			.Returns(fakeServiceScopeFactory);

		var fakeScope = Substitute.For<IServiceScope>();
		fakeScope.ServiceProvider
			.Returns(fakeServiceProvider);
		fakeServiceScopeFactory.CreateScope()
			.Returns(fakeScope);

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		fakeServiceProvider.GetService<ISerialNumberGenerator>()
			.Returns(fakeSerialNumberGenerator);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		fakeServiceProvider.GetService<ITgsRepository>()
			.Returns(fakeTgsRepository);

		await sut.PushCommandAsync("test1", command);

		await command.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.IsTrue(command.IsExcuted);
	}

	[TestMethod]
	public async Task BucketDispatcher_test1_建立30的Bucket_填入四次10_應該只填三次()
	{
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new BucketDispatcher(fakeServiceProvider);

		var fakeServiceScopeFactory = Substitute.For<IServiceScopeFactory>();
		fakeServiceProvider.GetService<IServiceScopeFactory>()
			.Returns(fakeServiceScopeFactory);

		var fakeScope = Substitute.For<IServiceScope>();
		fakeScope.ServiceProvider
			.Returns(fakeServiceProvider);
		fakeServiceScopeFactory.CreateScope()
			.Returns(fakeScope);

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		fakeServiceProvider.GetService<ISerialNumberGenerator>()
			.Returns(fakeSerialNumberGenerator);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		fakeServiceProvider.GetService<ITgsRepository>()
			.Returns(fakeTgsRepository);

		var createCommand = new CreateBucketCommand(30M, BucketType.Deposit, null);

		var command1 = new FillInBucketCommand(10M, FillType.Game, null);
		var command2 = new FillInBucketCommand(10M, FillType.Game, null);
		var command3 = new FillInBucketCommand(10M, FillType.Game, null);
		var command4 = new FillInBucketCommand(10M, FillType.Game, null);

		var createExpected = new BucketCreateInfo("test1", 30M, BucketType.Deposit, null);
		var fillExpected = new FillInBucketInfo(10M, FillType.Game, null, false);

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

		_ = fakeTgsRepository.Received(1)
			.AddPlayerBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(createExpected),
				Arg.Any<CancellationToken>());
		_ = fakeTgsRepository.Received(2)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(fillExpected),
				Arg.Any<CancellationToken>());
		_ = fakeTgsRepository.Received(1)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(fillExpected with { SetFull = true }),
				Arg.Any<CancellationToken>());
	}
}
