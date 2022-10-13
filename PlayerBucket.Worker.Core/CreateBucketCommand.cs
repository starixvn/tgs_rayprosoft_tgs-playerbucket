namespace Tgs.Service.PlayerBuckets;

public class CreateBucketCommand : IBucketCommand
{
	private readonly decimal _amount;
	private readonly BucketType _type;
	private readonly string? _externalId;
	private readonly TaskCompletionSource<Guid> _taskCompletionSource = new();

	public CreateBucketCommand(decimal amount, BucketType type, string? externalId)
	{
		_amount = amount;
		_type = type;
		_externalId = externalId;
	}

	public async ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
	{
		var createdId = await bucketStore.CreateBucketAsync(
			_amount,
			_type,
			_externalId,
			cancellationToken).ConfigureAwait(false);

		_taskCompletionSource.SetResult(createdId);
	}

	public Task<Guid> GetCreatedIdAsync() => _taskCompletionSource.Task;
}
