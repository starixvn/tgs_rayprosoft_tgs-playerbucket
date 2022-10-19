extern alias Client;
extern alias Server;

using Client::Tgs.PlayerBucket.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Server::Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.Repositories.Entities;

namespace Tgs.PlayerBucketWorker.IntegrationTests;

[TestClass]
public class CreateBucketTests
{
	[TestMethod]
	public async Task CreateBucketProcessor_建立一個Bucket()
	{
		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var client = new PlayerBucketClient(fakeClientMessageSender);

		var playerCode = "test1";
		var amount = 100M;
		var bucketType = Client::Tgs.PlayerBucket.Clients.BucketType.Deposit;
		var externalId = Guid.NewGuid().ToString();

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

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var createdId = await client.CreateBucketAsync(playerCode, amount, bucketType, externalId);

		_ = fakeBucketEventRepository.Received(1)
			.AddPlayerBucketAsync(
				Arg.Any<Guid>(),
				Arg.Is(createdId),
				Arg.Any<BucketCreateInfo>(),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task FillInBucketHandler_填充一個Bucket()
	{
		var fakeClientMessageSender = Substitute.For<IMessageSender>();
		var client = new PlayerBucketClient(fakeClientMessageSender);

		var playerCode = "test1";
		var amount = 100M;
		var fillType = Client::Tgs.PlayerBucket.Clients.FillType.Game;
		var externalId = Guid.NewGuid().ToString();

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
			Service.PlayerBuckets.FillType.Game,
			externalId,
			true);

		var bucketId = await client.CreateBucketAsync(
			playerCode,
			amount,
			Client::Tgs.PlayerBucket.Clients.BucketType.Deposit,
			externalId);
		await client.FillInBucketAsync(playerCode, amount, fillType, externalId);

		_ = fakeBucketEventRepository.Received(1)
			.FillInBucketAsync(
				Arg.Any<Guid>(),
				Arg.Is(bucketId),
				Arg.Is(expected),
				Arg.Any<CancellationToken>());
	}
}
