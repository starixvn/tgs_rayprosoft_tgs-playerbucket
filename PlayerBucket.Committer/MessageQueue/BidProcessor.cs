using Google.Protobuf;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class BidProcessor : IMessageProcessor
{
	private readonly IDistributedResourceManager _distributedResourceManager;

	public BidProcessor(IDistributedResourceManager distributedResourceManager)
	{
		_distributedResourceManager = distributedResourceManager ?? throw new ArgumentNullException(nameof(distributedResourceManager));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CommitBidRequest.Parser.ParseFrom(data.Span);

		var response = new BidResponse()
		{
			Token = await _distributedResourceManager
				.BidAsync(request.BucketId, Environment.MachineName, cancellationToken)
				.ConfigureAwait(false)
		};

		return response.ToByteArray();
	}
}
