using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

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
			Substitute.For<ITgsRepository>());

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

		var sut = new BucketCounter(
			"test1",
			fakeSerialNumberGenerator,
			fakeTgsRepository);

		var externalId = Guid.NewGuid().ToString();
		var command = new CreateBucketCommand(100M, BucketType.Deposit, externalId);

		var expected = new BucketCreateInfo("test1", 100M, BucketType.Deposit, externalId);

		await sut.PrepareAsync();
		await sut.PushCommandAsync(command);

		_ = await command.GetCreatedIdAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		_ = fakeTgsRepository.Received(1)
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

		var sut = new BucketCounter(
			"test1",
			fakeSerialNumberGenerator,
			fakeTgsRepository);

		var externalId = Guid.NewGuid().ToString();
		var command = new FillInBucketCommand(100M, FillType.Game, externalId);

		var expected = new FillInBucketInfo(100M, FillType.Game, externalId, false);

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

		_ = fakeTgsRepository.Received(1)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Is(expected),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task BucketCounter_Prepare兩次不會出錯()
	{
		var sut = new BucketCounter(
			"test1",
			Substitute.For<ISerialNumberGenerator>(),
			Substitute.For<ITgsRepository>());

		var command = new StubBucketCommand();

		await sut.PrepareAsync();
		await sut.PrepareAsync();
		await sut.PushCommandAsync(command);

		await command.WaitAsync().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.IsTrue(command.IsExcuted);
	}
}
