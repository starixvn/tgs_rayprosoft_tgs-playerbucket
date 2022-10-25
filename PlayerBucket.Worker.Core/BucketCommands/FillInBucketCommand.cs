namespace Tgs.Service.PlayerBuckets.BucketCommands;

public class FillInBucketCommand : IBucketCommand
{
	private readonly decimal _amount;
	private readonly FillType _type;
	private readonly string? _externalId;
	private readonly TaskCompletionSource _taskCompletionSource = new();

	public FillInBucketCommand(decimal amount, FillType type, string? externalId)
	{
		_amount = amount;
		_type = type;
		_externalId = externalId;
	}

	public async ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
	{
		var remainingAmount = _amount;

		while (remainingAmount > 0)
		{
			var bucket = await bucketStore
				.GetNotFullBucketAsync(cancellationToken)
				.ConfigureAwait(false);

			if (bucket == null)
				break;

			var bucketNotFillAmount = bucket.Amount - bucket.FillInAmount;

			var fillInfo = bucketNotFillAmount < remainingAmount
				? new FillInBucketInfo(
					bucketStore.PlayerCode,
					bucketNotFillAmount,
					_type,
					_externalId,
					true)
				: new FillInBucketInfo(
					bucketStore.PlayerCode,
					remainingAmount,
					_type,
					_externalId,
					bucketNotFillAmount == remainingAmount);

			remainingAmount -= fillInfo.Amount;

			await bucketStore.FillInBucketAsync(
				bucket,
				fillInfo,
				cancellationToken).ConfigureAwait(false);
		}

		_taskCompletionSource.SetResult();
	}

	public Task WaitAsync() => _taskCompletionSource.Task;

	public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
		=> _taskCompletionSource.Task.WaitAsync(timeout, cancellationToken);
}
