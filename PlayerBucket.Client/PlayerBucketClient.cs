using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Service.PlayerBuckets;
using Tgs.Services.PlayerBucket.Protos;

namespace Tgs.PlayerBucket.Clients;

internal class PlayerBucketClient : IPlayerBucketClient
{
	private readonly IMessageSender _messageSender;

	public PlayerBucketClient(IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<Guid> CreateBucketAsync(
		string playerCode,
		decimal amount,
		BucketType type,
		string? externalId,
		CancellationToken cancellationToken = default)
	{
		var request = new CreateBucketRequest
		{
			PlayerCode = playerCode,
			Amount = amount,
			BucketType = type.ToString(),
			ExternalId = externalId
		};

		var result = await _messageSender.RequestAsync(
			"tgs.bucket.create",
			request.ToByteArray(),
			cancellationToken).ConfigureAwait(false);

		var response = CreateBucketResponse.Parser.ParseFrom(result.Span);

		return response.BucketId.ToGuid();
	}

	public ValueTask FillInBucketAsync(
		string playerCode,
		decimal amount,
		FillType type,
		string? externalId,
		CancellationToken cancellationToken = default)
	{
		var request = new FillInRequest
		{
			PlayerCode = playerCode,
			Amount = amount,
			FillType = type.ToString(),
			ExternalId = externalId
		};

		return _messageSender.SendAsync(
			"tgs.bucket.fill",
			request.ToByteArray(),
			cancellationToken);
	}

	public ValueTask SnapshotBucketAsync(string playerCode, Guid bucketId, CancellationToken cancellationToken = default)
	{
		var request = new SnapshotRequest
		{
			BucketId = bucketId,
			PlayerCode = playerCode
		};

		return _messageSender.PublishAsync("tgs.bucket.snapshot", request.ToByteArray(), cancellationToken);
	}

	public async ValueTask<decimal> GetBucketAmountAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var request = new GetBucketAmountRequest
		{
			PlayerCode = playerCode
		};

		var result = await _messageSender.RequestAsync(
			"tgs.bucket.amount",
			request.ToByteArray(),
			cancellationToken).ConfigureAwait(false);

		var response = GetBucketAmountResponse.Parser.ParseFrom(result.Span);

		return response.Amount;
	}
}
