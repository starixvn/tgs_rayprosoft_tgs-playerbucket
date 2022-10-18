extern alias Client;
extern alias Server;

using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NATS.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Server::Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucketWorker.IntegrationTests;

[TestClass]
public class BidTests
{
	[TestMethod]
	public async Task BidProcessor_會透過MQ呼叫DistributedResourceManager來決定一個處理者()
	{
		var fakeDistributedResourceManager = Substitute.For<IDistributedResourceManager>();

		var sut = new BidProcessor(fakeDistributedResourceManager);

		var request = new BidRequest
		{
			PlayerCode = "test1"
		};

		var expected = "aaa";
		_ = fakeDistributedResourceManager.BidAsync(Arg.Is(request.PlayerCode), Arg.Any<string>())
			.Returns(expected);

		var actual = await sut.HandleAsync(request.ToByteArray());

		var response = BidResponse.Parser.ParseFrom(actual.Span);

		Assert.AreEqual(expected, response.Token);
	}

	[TestMethod]
	public async Task BucketRequestDispatcher_進行Bid會透過MQ來取得接下來處理的subject參數()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();

		var sut = new MockBucketRequestDispatcher(
			fakeMessageSender,
			fakeDistributeResourceManager,
			NullLogger<MockBucketRequestDispatcher>.Instance);

		var playerCode = "test1";

		_ = await sut.InvokeGetBidTokenAsync(playerCode);

		_ = fakeMessageSender.Received(1)
			.RequestAsync(
				Arg.Is("tgs.bucket.bid"),
				Arg.Any<ReadOnlyMemory<byte>>());
	}

	[TestMethod]
	public async Task BucketRequestDispatcher_進行Bid如果發生NATSNoRespondersException_會重試一次()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();

		var sut = new MockBucketRequestDispatcher(
			fakeMessageSender,
			fakeDistributeResourceManager,
			NullLogger<MockBucketRequestDispatcher>.Instance);

		var playerCode = "test1";

		_ = fakeMessageSender.RequestAsync(Arg.Is("tgs.bucket.bid"), Arg.Any<ReadOnlyMemory<byte>>())
			.Throws(new NATSNoRespondersException())
			.AndDoes(_ => fakeMessageSender.RequestAsync(Arg.Is("tgs.bucket.bid"), Arg.Any<ReadOnlyMemory<byte>>())
				.Returns(new BidResponse
				{
					Token = "aaa"
				}.ToByteArray()));

		var actual = await sut.InvokeGetBidTokenAsync(playerCode);

		Assert.AreEqual("aaa", actual);
	}

	[TestMethod]
	public async Task BucketRequestDispatcher_進行Bid如果已經有得標者_直接回傳token()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakeDistributeResourceManager = Substitute.For<IDistributedResourceManager>();

		var sut = new MockBucketRequestDispatcher(
			fakeMessageSender,
			fakeDistributeResourceManager,
			NullLogger<MockBucketRequestDispatcher>.Instance);

		var playerCode = "test1";

		_ = fakeDistributeResourceManager.GetBidTokenAsync(
			Arg.Is(playerCode))
			.Returns("aaa");

		var actual = await sut.InvokeGetBidTokenAsync(playerCode);

		Assert.AreEqual("aaa", actual);

		_ = fakeMessageSender.DidNotReceive()
			.RequestAsync(
				Arg.Is("tgs.bucket.bid"),
				Arg.Any<ReadOnlyMemory<byte>>());
	}
}
