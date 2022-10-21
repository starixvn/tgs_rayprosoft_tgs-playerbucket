using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Channels;
using Json.Patch;
using Microsoft.Extensions.Logging;
using Tgs.SerialNumberGenerator;

namespace Tgs.Service.PlayerBuckets;

internal class BucketCounter : IBucketStore, IDisposable, IAsyncDisposable
{
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly ITgsRepository _tgsRepository;
	private readonly IBucketEventRepository _bucketEventRepository;
	private readonly IEventSource _eventSource;
	private readonly ILogger<BucketCounter> _logger;
	private readonly List<BucketInfo> _buckets = new();

	private readonly Channel<IBucketCommand> _channel = Channel.CreateUnbounded<IBucketCommand>(
		new UnboundedChannelOptions
		{
			SingleReader = true
		});

	private readonly SemaphoreSlim _locker = new(1, 1);
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private Task? _backgroundTask;

	private bool _disposedValue;

	public event EventHandler? Disposed;

	public string PlayerCode { get; }

	public BucketCounter(
		string playerCode,
		ISerialNumberGenerator serialNumberGenerator,
		ITgsRepository tgsRepository,
		IBucketEventRepository bucketEventRepository,
		IEventSource eventSource,
		ILogger<BucketCounter> logger)
	{
		if (string.IsNullOrWhiteSpace(playerCode))
			throw new ArgumentException($"'{nameof(playerCode)}' 不得為 Null 或空白字元。", nameof(playerCode));

		PlayerCode = playerCode;
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_bucketEventRepository = bucketEventRepository ?? throw new ArgumentNullException(nameof(bucketEventRepository));
		_eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
			_ = _locker.Release();
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

		await _bucketEventRepository.AddPlayerBucketAsync(
			createId,
			bucketId,
			new BucketCreateInfo(
				PlayerCode,
				amount,
				type,
				externalId),
			cancellationToken).ConfigureAwait(false);

		_buckets.Add(new BucketInfo(
			bucketId,
			PlayerCode,
			amount,
			type,
			0M,
			false,
			createId));

		await _eventSource.PublishBucketChangedAsync(
			new BucketChangedEvent(bucketId),
			cancellationToken).ConfigureAwait(false);

		return bucketId;
	}

	async ValueTask<BucketInfo?> IBucketStore.GetNotFullBucketAsync(CancellationToken cancellationToken)
	{
		if (_buckets.Count == 0)
			return null;

		var bucket = _buckets[0];

		var snapshotBucket = await MakeSnapshotAsync(
			bucket.Id,
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
	{
		var newEventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		await _bucketEventRepository.FillInBucketAsync(
			newEventId,
			bucket.Id,
			fillInInfo,
			cancellationToken).ConfigureAwait(false);

		var newBucket = fillInInfo.ApplyPatch(bucket) with
		{
			SnapshotId = newEventId
		};

		if (newBucket.IsFull)
			_ = _buckets.Remove(bucket);
		else
			_buckets[0] = newBucket;

		await _eventSource.PublishBucketChangedAsync(
			new BucketChangedEvent(bucket.Id),
			cancellationToken).ConfigureAwait(false);
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
				Disposed?.Invoke(this, EventArgs.Empty);
			}

			_disposedValue = true;
		}
	}

	private async ValueTask PrepareNotFullBucketsAsync(CancellationToken cancellationToken = default)
	{
		var databaeAllNotFullBuckets = await _tgsRepository
			.GetPlayerNotFullBucketsAsync(PlayerCode, cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var tempAllBucketIds = await _bucketEventRepository
			.GetPlayerCreatedBucketIdsAsync(PlayerCode, cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var notYetInDatabaseIds = from id in tempAllBucketIds
								  join bucket in databaeAllNotFullBuckets
									on id equals bucket.Id into g
								  from bucket in g.DefaultIfEmpty()
								  where bucket is null
								  select id;

		var tempBuckets = notYetInDatabaseIds.ToAsyncEnumerable()
			.SelectMany(id => MakeSnapshotAsync(
				id,
				null,
				cancellationToken).AsTask().ToAsyncEnumerable());

		await foreach (var bucket in databaeAllNotFullBuckets.ToAsyncEnumerable().Union(tempBuckets)
			.OrderBy(bucket => bucket.Id)
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
			_buckets.Add(bucket);
	}

	private async Task ProcessCommandLoopAsync(CancellationToken cancellationToken = default)
	{
		await Task.Yield();

		var reader = _channel.Reader;
		while (await reader
			.WaitToReadAsync(cancellationToken)
			.ConfigureAwait(false))
		{
			if (reader.TryRead(out var command))
				try
				{
					await CountAsync(
						command,
						cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Process command occur errors.");
				}
		}
	}

	private ValueTask CountAsync(IBucketCommand command, CancellationToken cancellationToken = default)
		=> command.ExecuteAsync(this, cancellationToken);

	private async ValueTask<BucketInfo> MakeSnapshotAsync(Guid bucketId, BucketInfo? source, CancellationToken cancellationToken = default)
	{
		var bucketEvents = source switch
		{
			_ when source is null => _tgsRepository
				.GetBucketAllEventsAsync(bucketId, cancellationToken)
				.Union(_bucketEventRepository.GetBucketAllEventsAsync(
					bucketId,
					cancellationToken)),
			_ when source is not null && source.SnapshotId is null => _tgsRepository.GetBucketAllEventsAsync(
				bucketId,
				cancellationToken),
			_ when source is not null && source.SnapshotId.HasValue => _tgsRepository
				.GetBucketAllEventsAsync(source.Id, source.SnapshotId.Value, cancellationToken)
				.Union(_bucketEventRepository.GetBucketAllEventsAsync(
					source.Id,
					source.SnapshotId.Value,
					cancellationToken)),
			_ => throw new Exception($"Can't create snapshot, BucketId: {bucketId}")
		};

		var snapshotId = Guid.Empty;
		var usedAmount = 0M;
		var snapshotBucket = source;

		await foreach (var evt in bucketEvents.OrderBy(evt => evt.Id)
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			var evtPatch = evt.ChangeSets.Deserialize<JsonPatch>()!;
			snapshotBucket = evtPatch.Apply(snapshotBucket, PlayerBucketUtility.SerializerOptions);

			snapshotId = evt.Id;
			if (evt.ChangeAmount < 0M)
				usedAmount -= evt.ChangeAmount;
		}

		return snapshotBucket! with
		{
			SnapshotId = snapshotId,
			FillInAmount = (source?.FillInAmount ?? 0M) + usedAmount
		};
	}

	// // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
	// ~BucketCounter()
	// {
	//     // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
	//     Dispose(disposing: false);
	// }
}
