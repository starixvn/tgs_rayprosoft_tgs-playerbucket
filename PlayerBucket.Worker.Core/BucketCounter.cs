using System.Text.Json;
using System.Threading.Channels;
using Json.Patch;
using Tgs.SerialNumberGenerator;

namespace Tgs.Service.PlayerBuckets;

internal class BucketCounter : IBucketStore, IDisposable, IAsyncDisposable
{
	private readonly string _playerCode;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly ITgsRepository _tgsRepository;
	private readonly List<BucketInfo> _buckets = new();
	private readonly Channel<IBucketCommand> _channel = Channel.CreateUnbounded<IBucketCommand>();
	private readonly SemaphoreSlim _locker = new(1, 1);
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private Task? _backgroundTask;

	private bool _disposedValue;

	public event EventHandler? Disposed;

	public BucketCounter(
		string playerCode,
		ISerialNumberGenerator serialNumberGenerator,
		ITgsRepository tgsRepository)
	{
		if (string.IsNullOrWhiteSpace(playerCode))
			throw new ArgumentException($"'{nameof(playerCode)}' 不得為 Null 或空白字元。", nameof(playerCode));

		_playerCode = playerCode;
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public ValueTask PushCommandAsync(IBucketCommand command, CancellationToken cancellationToken = default)
		=> _channel.Writer.WriteAsync(
			command,
			CancellationTokenSource.CreateLinkedTokenSource(
				cancellationToken,
				_cancellationTokenSource.Token).Token);

	public async ValueTask PrepareAsync(CancellationToken cancellationToken = default)
	{
		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

		if (_backgroundTask is not null)
			return;

		await _locker.WaitAsync(cts.Token);

		try
		{
			if (_backgroundTask is not null)
				return;

			await PrepareNotFullBucketsAsync(cts.Token).ConfigureAwait(false);

			_backgroundTask = Task.Run(
				() => ProcessCommandLoopAsync(cts.Token),
				cts.Token);
		}
		finally
		{
			_locker.Release();
		}
	}

	async ValueTask<Guid> IBucketStore.CreateBucketAsync(decimal amount, BucketType type, string? externalId, CancellationToken cancellationToken)
	{
		var createId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);
		var bucketId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		await _tgsRepository.AddPlayerBucketAsync(
			createId,
			bucketId,
			new BucketCreateInfo(
				_playerCode,
				amount,
				type,
				null),
			cancellationToken).ConfigureAwait(false);

		_buckets.Add(new BucketInfo(
			bucketId,
			_playerCode,
			amount,
			type,
			0M,
			false,
			createId));

		return bucketId;
	}

	async ValueTask<BucketInfo?> IBucketStore.GetNotFullBucketAsync(CancellationToken cancellationToken)
	{
		if (_buckets.Count == 0)
			return null;

		var bucket = _buckets[0];

		var snapshotBucket = await MakeSnapshotAsync(
			bucket,
			cancellationToken).ConfigureAwait(false);

		if (snapshotBucket.IsFull is true || snapshotBucket.FillInAmount >= snapshotBucket.Amount)
		{
			_buckets.RemoveAt(0);

			return await ((IBucketStore)this).GetNotFullBucketAsync(cancellationToken).ConfigureAwait(false);
		}

		if (snapshotBucket.SnapshotId != bucket.SnapshotId)
			_buckets[0] = snapshotBucket;

		return snapshotBucket;
	}

	async ValueTask IBucketStore.FillInBucketAsync(BucketInfo bucket, FillInBucketInfo fillInInfo, CancellationToken cancellationToken)
		=> await _tgsRepository.FillInBucketAsync(
			await _serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false),
			bucket.Id,
			fillInInfo,
			cancellationToken).ConfigureAwait(false);

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
				Disposed?.Invoke(this, EventArgs.Empty);
			}

			_disposedValue = true;
		}
	}

	private async ValueTask PrepareNotFullBucketsAsync(CancellationToken cancellationToken = default)
	{
		var allNotFullBuckets = _tgsRepository.GetPlayerNotFullBucketsAsync(_playerCode, cancellationToken);

		await foreach (var bucket in allNotFullBuckets.WithCancellation(cancellationToken).ConfigureAwait(false))
			_buckets.Add(bucket);
	}

	private async Task ProcessCommandLoopAsync(CancellationToken cancellationToken = default)
	{
		while (await _channel.Reader
			.WaitToReadAsync(cancellationToken)
			.ConfigureAwait(false))
		{
			if (_channel.Reader.TryRead(out var command))
				await CountAsync(
					command,
					cancellationToken).ConfigureAwait(false);
		}
	}

	private ValueTask CountAsync(IBucketCommand command, CancellationToken cancellationToken = default)
		=> command.ExecuteAsync(this, cancellationToken);

	private async ValueTask<BucketInfo> MakeSnapshotAsync(BucketInfo source, CancellationToken cancellationToken = default)
	{
		var bucketEvents = source.SnapshotId is null
			? _tgsRepository.GetBucketAllEventsAsync(source.Id, cancellationToken)
			: _tgsRepository.GetBucketAllEventsAsync(source.Id, source.SnapshotId.Value, cancellationToken);

		var snapshotId = Guid.Empty;
		var usedAmount = 0M;
		var snapshotBucket = default(BucketInfo);

		await foreach (var evt in bucketEvents
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			var evtPatch = evt.ChangeSets.Deserialize<JsonPatch>()!;
			snapshotBucket = evtPatch.Apply(snapshotBucket, PlayerBucketUtility.SerializerOptions);

			snapshotId = evt.Id;
			if (evt.ChangeAmount < 0M)
				usedAmount += evt.ChangeAmount;
		}

		return snapshotBucket! with
		{
			SnapshotId = snapshotId,
			FillInAmount = usedAmount
		};
	}

	// // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
	// ~BucketCounter()
	// {
	//     // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
	//     Dispose(disposing: false);
	// }
}
