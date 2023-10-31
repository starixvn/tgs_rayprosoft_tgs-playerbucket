using Google.Protobuf;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;
using Valhalla.MessageQueue;

namespace Tgs.PlayerBucketWorker.MessageQueue;

public class BidProcessor : IMessageProcessor
{
	private readonly IDistributedResourceManager _distributedResourceManager;

	public BidProcessor(IDistributedResourceManager distributedResourceManager)
	{
		_distributedResourceManager = distributedResourceManager ?? throw new ArgumentNullException(nameof(distributedResourceManager));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = BidRequest.Parser.ParseFrom(data.Span);

		var response = new BidResponse()
		{
			Token = await _distributedResourceManager
				.BidAsync(request.PlayerCode, Environment.MachineName, cancellationToken)
				.ConfigureAwait(false)
		};

		return response.ToByteArray();
	}
}
