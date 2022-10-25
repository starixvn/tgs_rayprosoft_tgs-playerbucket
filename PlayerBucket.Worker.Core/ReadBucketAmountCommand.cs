namespace Tgs.Service.PlayerBuckets;

public class ReadBucketAmountCommand : IBucketCommand
{
	private readonly TaskCompletionSource<decimal> _taskCompletionSource = new();

	public async ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
	{
		try
		{
			var amount = await bucketStore.GetBucketAmountAsync(cancellationToken).ConfigureAwait(false);

			_taskCompletionSource.SetResult(amount);
		}
		catch (Exception ex)
		{
			_taskCompletionSource.SetException(ex);
		}
	}

	public Task<decimal> GetAmountAsync() => _taskCompletionSource.Task;
}
