namespace Tgs.Service.PlayerBuckets.BucketCommands;

public class ReadBucketAmountCommand : IBucketCommand
{
	private readonly TaskCompletionSource<ReadBucketResult> _taskCompletionSource = new();

	public async ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
	{
		try
		{
			var result = await bucketStore.GetBucketAmountAsync(cancellationToken).ConfigureAwait(false);

			_taskCompletionSource.SetResult(result);
		}
		catch (Exception ex)
		{
			_taskCompletionSource.SetException(ex);
		}
	}

	public Task<ReadBucketResult> GetAmountAsync() => _taskCompletionSource.Task;
}
