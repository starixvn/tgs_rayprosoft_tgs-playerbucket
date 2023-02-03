extern alias Client;
extern alias Server;

using Client::Tgs.PlayerBucket.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Server::Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.MessageQueue;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucketWorker.IntegrationTests;

[TestClass]
public class BucketTests
{
	[TestMethod]
	public async Task CreateBucketProcessor_建立一個Bucket()
	{
		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var client = new PlayerBucketClient(new HttpClient(), fakeClientMessageSender);

		var playerCode = "test1";
		var amount = 100M;
		var bucketType = BucketType.Deposit;
		var externalId = Guid.NewGuid().ToString();
		var remark = "remark";

		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		await using var web = PlayerBucketWorkerTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddSimpleConsole())
				.ConfigureServices(services => services
					.AddFakeNatsMessageQueue()
					.Replace(ServiceDescriptor.Singleton(sp =>
					{
						var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
						_ = dbContextOptionsBuilder
							.UseInMemoryDatabase(Guid.NewGuid().ToString())
							.ConfigureWarnings(x => x.Ignore(
								InMemoryEventId.TransactionIgnoredWarning));

						return new TgsContext(dbContextOptionsBuilder.Options);
					}))
					.Replace(ServiceDescriptor.Singleton(fakeBucketEventRepository))));

		_ = fakeClientMessageSender.RequestAsync(
			Arg.Is("tgs.bucket.create"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<CreateBucketProcessor>(web.Services);

				return processor.HandleAsync(data);
			});

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var createdId = await client.CreateBucketAsync(playerCode, amount, bucketType, externalId, remark);

		_ = fakeBucketEventRepository.Received(1)
			.AddPlayerBucketAsync(
				Arg.Any<Guid>(),
				Arg.Is(createdId),
				Arg.Any<BucketCreateInfo>(),
				Arg.Is(remark),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task FillInBucketHandler_填充一個Bucket()
	{
		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var client = new PlayerBucketClient(new HttpClient(), fakeClientMessageSender);

		var playerCode = "test1";
		var amount = 100M;
		var fillType = FillType.Game;
		var externalId = Guid.NewGuid().ToString();
		var remark = "remark";

		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		await using var web = PlayerBucketWorkerTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddFakeNatsMessageQueue()
					.Replace(ServiceDescriptor.Singleton(sp =>
					{
						var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
						_ = dbContextOptionsBuilder
							.UseInMemoryDatabase(Guid.NewGuid().ToString())
							.ConfigureWarnings(x => x.Ignore(
								InMemoryEventId.TransactionIgnoredWarning));

						return new TgsContext(dbContextOptionsBuilder.Options);
					}))
					.Replace(ServiceDescriptor.Singleton(fakeBucketEventRepository))));

		_ = fakeClientMessageSender.RequestAsync(
			Arg.Is("tgs.bucket.create"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<CreateBucketProcessor>(web.Services);

				return processor.HandleAsync(data);
			});

		_ = fakeClientMessageSender.SendAsync(
			Arg.Is("tgs.bucket.fill"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<FillInBucketHandler>(web.Services);

				return processor.HandleAsync(data);
			});

		var expected = new FillInBucketInfo(
			playerCode,
			amount,
			FillType.Game,
			externalId,
			true);

		var bucketId = await client.CreateBucketAsync(
			playerCode,
			amount,
			BucketType.Deposit,
			externalId,
			null);
		await client.FillInBucketAsync(playerCode, amount, fillType, externalId, remark);

		_ = fakeBucketEventRepository.Received(1)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Is(bucketId),
				Arg.Is(expected),
				Arg.Is(remark),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task GetBucketAmountProcessor_查詢玩家洗碼量()
	{
		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var client = new PlayerBucketClient(new HttpClient(), fakeClientMessageSender);

		var playerCode = "test1";

		var fakeBucketEventRepository = Substitute.For<IBucketEventRepository>();

		await using var web = PlayerBucketWorkerTestHost.Instance
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureServices(services => services
					.AddFakeNatsMessageQueue()
					.Replace(ServiceDescriptor.Singleton(sp =>
					{
						var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
						_ = dbContextOptionsBuilder
							.UseInMemoryDatabase(Guid.NewGuid().ToString())
							.ConfigureWarnings(x => x.Ignore(
								InMemoryEventId.TransactionIgnoredWarning));

						return new TgsContext(dbContextOptionsBuilder.Options);
					}))
					.Replace(ServiceDescriptor.Singleton(fakeBucketEventRepository))));

		_ = fakeClientMessageSender.RequestAsync(
			Arg.Is("tgs.bucket.amount"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<GetBucketAmountProcessor>(web.Services);

				return processor.HandleAsync(data);
			});

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var actual = await client.ReadBucketsAsync(playerCode).AsTask().WaitAsync(TimeSpan.FromSeconds(1D));

		Assert.AreEqual(0M, actual.Amount);
	}
}
