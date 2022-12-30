using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Service.PlayerBuckets;

internal class BucketDispatcher : IBucketDispatcher, IDisposable, IAsyncDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ConcurrentDictionary<string, BucketCounter> _counters = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private bool _disposedValue;

	public BucketDispatcher(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_ = Task.Run(async () =>
		{
			while (true)
			{
				PlayerBucketMetrics._ProcessPlayers.Record(_counters.Count);

				await Task.Delay(
					1000,
					_cancellationTokenSource.Token).ConfigureAwait(false);
			}
		}, _cancellationTokenSource.Token);
	}

	public async Task PushCommandAsync(string playerCode, IBucketCommand command, CancellationToken cancellationToken = default)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

		var counter = _counters.GetOrAdd(
			playerCode,
			key =>
			{
				var scope = _serviceProvider.CreateScope();
				var newCounter = ActivatorUtilities.CreateInstance<BucketCounter>(
					scope.ServiceProvider,
					key);

				newCounter.Disposed += (_, _) =>
				{
					_ = _counters.TryRemove(newCounter.PlayerCode, out _);
					scope.Dispose();
				};

				return newCounter;
			});

		await counter.PrepareAsync(cts.Token).ConfigureAwait(false);
		await counter.PushCommandAsync(command, cts.Token).ConfigureAwait(false);
	}

	public void ExtendExpiry(string playerCode)
	{
		if (_counters.TryGetValue(playerCode, out var counter))
			counter.ExtendExpiry();
	}

	public ValueTask DisposeAsync()
	{
		Dispose();

		return ValueTask.CompletedTask;
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_cancellationTokenSource.Cancel();
			}

			_disposedValue = true;
		}
	}
}
