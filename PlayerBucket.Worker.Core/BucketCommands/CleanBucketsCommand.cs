namespace Tgs.Service.PlayerBuckets.BucketCommands;

internal class CleanBucketsCommand : IBucketCommand
{
	private readonly Guid _eventId;

	public CleanBucketsCommand(Guid eventId)
	{
		_eventId = eventId;
	}

	public ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
	{
		return bucketStore.CleanUpBucketsAsync(_eventId, cancellationToken);
	}
}
