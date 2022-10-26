namespace Tgs.Service.PlayerBuckets.BucketCommands;

public class CleanBucketsCommand : IBucketCommand
{
	private readonly Guid _anchorBucketId;
	private readonly string? _externalId;

	public CleanBucketsCommand(Guid anchorBucketId, string? externalId)
	{
		_anchorBucketId = anchorBucketId;
		_externalId = externalId;
	}

	public ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
		=> bucketStore.CleanUpBucketsAsync(_anchorBucketId, _externalId, cancellationToken);
}
