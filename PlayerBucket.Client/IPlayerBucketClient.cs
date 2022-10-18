namespace Tgs.PlayerBucket.Clients;

public interface IPlayerBucketClient
{
	ValueTask<Guid> CreateBucketAsync(
		string playerCode,
		decimal amount,
		BucketType type,
		string? externalId,
		CancellationToken cancellationToken = default);

	ValueTask FillInBucketAsync(
		string playerCode,
		decimal amount,
		FillType type,
		string? externalId,
		CancellationToken cancellationToken = default);
}
