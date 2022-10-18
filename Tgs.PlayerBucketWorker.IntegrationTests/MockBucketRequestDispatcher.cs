extern alias Server;

using Microsoft.Extensions.Logging;
using Server::Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucketWorker.IntegrationTests;

internal class MockBucketRequestDispatcher : BucketRequestDispatcher
{
	public MockBucketRequestDispatcher(
		IMessageSender messageSender,
		IDistributedResourceManager distributedResourceManager,
		ILogger logger)
		: base(messageSender, distributedResourceManager, logger)
	{
	}

	public Task<string> InvokeGetBidTokenAsync(string playerCode, CancellationToken cancellationToken = default)
		=> GetBidTokenAsync(playerCode, cancellationToken);
}
