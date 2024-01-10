extern alias Server;

using Microsoft.Extensions.Logging;
using Server::Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.IntegrationTests;

internal class MockBucketRequestDispatcher(
	IMessageSender messageSender,
	IDistributedResourceManager distributedResourceManager,
	ILogger logger) : BucketRequestDispatcher(messageSender, distributedResourceManager, logger)
{
	public Task<string> InvokeGetBidTokenAsync(string playerCode, CancellationToken cancellationToken = default)
		=> GetBidTokenAsync(playerCode, cancellationToken);
}
