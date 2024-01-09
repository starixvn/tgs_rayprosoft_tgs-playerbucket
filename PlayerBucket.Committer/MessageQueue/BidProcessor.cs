using Google.Protobuf;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBuckets.Committer.MessageQueue;

public class BidProcessor : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly IDistributedResourceManager _distributedResourceManager;

	public BidProcessor(IDistributedResourceManager distributedResourceManager)
	{
		_distributedResourceManager = distributedResourceManager ?? throw new ArgumentNullException(nameof(distributedResourceManager));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
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
