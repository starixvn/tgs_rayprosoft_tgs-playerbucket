using Tgs.Service.PlayerBuckets;

namespace Tgs.PlayerBucket.Client;

public interface IPlayerBucketForTypesClient
{
	ValueTask CreateBucketForBucketTypesAsync(
		string playerCode,
		decimal amount,
		BucketType type,
		string? externalId,
		string? remark,
		IEnumerable<string>? bucketTypes = null,
		CancellationToken cancellationToken = default);

	ValueTask FillInBucketForBucketTypesAsync(
		string playerCode,
		decimal amount,
		FillType type,
		string? externalId,
		string? remark,
		IEnumerable<string>? bucketTypes = null,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<(string BucketName, ReadBucketResult Result)> ReadBucketsForBucketTypesAsync(
		string playerCode,
		IEnumerable<string>? bucketTypes = null,
		CancellationToken cancellationToken = default);

	ValueTask CleanBucketsForBucketTypesAsync(
		string playerCode,
		IEnumerable<(string? BucketType, Guid AnchorBucketId)> anchorBucketIdsByType,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken = default);
}
