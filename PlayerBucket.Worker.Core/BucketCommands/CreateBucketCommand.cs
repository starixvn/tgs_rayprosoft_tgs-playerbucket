namespace Tgs.Service.PlayerBuckets.BucketCommands;

public class CreateBucketCommand(decimal amount, BucketType type, string? externalId, string? remark) : IBucketCommand
{
	private readonly decimal _amount = amount;
	private readonly BucketType _type = type;
	private readonly string? _externalId = externalId;
	private readonly string? _remark = remark;
	private readonly TaskCompletionSource<Guid> _taskCompletionSource = new();

	public async ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
	{
		var createdId = await bucketStore.CreateBucketAsync(
			_amount,
			_type,
			_externalId,
			_remark,
			cancellationToken).ConfigureAwait(false);

		_taskCompletionSource.SetResult(createdId);
	}

	public Task<Guid> GetCreatedIdAsync() => _taskCompletionSource.Task;
}
