namespace Tgs.Service.PlayerBuckets.BucketCommands;

public class CleanBucketsCommand : IBucketCommand
{
	private readonly Guid _anchorBucketId;
	private readonly string? _externalId;
	private readonly string? _remark;

	public CleanBucketsCommand(Guid anchorBucketId, string? externalId, string? remark)
	{
		_anchorBucketId = anchorBucketId;
		_externalId = externalId;
		_remark = remark;
	}

	public ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
		=> bucketStore.CleanUpBucketsAsync(
			_anchorBucketId,
			_externalId,
			_remark,
			cancellationToken);
}
