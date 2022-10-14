using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Worker.Core.UnitTests;

internal class StubBucketCommand : IBucketCommand
{
	private TaskCompletionSource _completionSource = new();

	public bool IsExcuted { get; private set; }

	public ValueTask ExecuteAsync(IBucketStore bucketStore, CancellationToken cancellationToken = default)
	{
		IsExcuted = true;

		_completionSource.SetResult();

		return ValueTask.CompletedTask;
	}

	public Task WaitAsync() => _completionSource.Task;
}
