namespace Tgs.Service.PlayerBuckets.BucketCommands;

public class CleanBucketsCommand(Guid anchorBucketId, string? externalId, string? remark) : IBucketCommand
{
	private readonly Guid _anchorBucketId = anchorBucketId;
	private readonly string? _externalId = externalId;
	private readonly string? _remark = remark;

	public ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
		=> bucketStore.CleanUpBucketsAsync(
			_anchorBucketId,
			_externalId,
			_remark,
			cancellationToken);
}
