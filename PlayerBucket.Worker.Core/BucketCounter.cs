using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Json.Patch;
using Microsoft.Extensions.Logging;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets.BucketCommands;

namespace Tgs.Service.PlayerBuckets;

internal class BucketCounter : IBucketStore, IDisposable, IAsyncDisposable
{
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly ITgsRepository _tgsRepository;
	private readonly IBucketEventRepository _bucketEventRepository;
	private readonly IEventSource _eventSource;
	private readonly List<BucketInfo> _buckets = [];

	private readonly Channel<IBucketCommand> _channel = Channel.CreateUnbounded<IBucketCommand>(
		new UnboundedChannelOptions
		{
			SingleReader = true
		});

	private readonly SemaphoreSlim _locker = new(1, 1);
	private readonly ILogger<BucketCounter> _logger;
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private decimal _bucketAmount = 0M;
	private Task? _backgroundTask;
	private int _expiry = 610;
	private Timer? _timer;
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

	~BucketCounter()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: false);
	}

	public async ValueTask PushCommandAsync(IBucketCommand command, CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken,
			_cancellationTokenSource.Token);

		await _channel.Writer.WriteAsync(
			command,
			cts.Token).ConfigureAwait(false);
	}

	public void ExtendExpiry()
	{
		_ = Interlocked.Exchange(ref _expiry, 610);
	}

	public async ValueTask PrepareAsync(CancellationToken cancellationToken = default)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

		if (_backgroundTask is not null)
			return;

		await _locker.WaitAsync(cts.Token).ConfigureAwait(false);

		try
		{
			if (_backgroundTask is not null)
				return;

			await PrepareNotFullBucketsAsync(cts.Token).ConfigureAwait(false);

			await ProcessMigrateAsync(cts.Token).ConfigureAwait(false);

			_timer = new Timer(_ =>
			{
				_ = Interlocked.Decrement(ref _expiry);

				if (_expiry <= 0)
					Dispose();
			}, null, 0, 1000);

			_backgroundTask = Task.Run(
				() => ProcessCommandLoopAsync(_cancellationTokenSource.Token),
				_cancellationTokenSource.Token);
		}
		finally
		{
			_ = _locker.Release();
		}
	}

	async ValueTask<Guid> IBucketStore.CreateBucketAsync(
		decimal amount,
		BucketType type,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken)
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
			remark,
			cancellationToken).ConfigureAwait(false);

		_buckets.Add(new BucketInfo(
			bucketId,
			PlayerCode,
			amount,
			type,
			0M,
			amount <= 0M,
			DateTime.Now,
			createId));

		_bucketAmount += amount;

		await _eventSource.PublishBucketChangedAsync(
			new BucketChangedEvent(bucketId, PlayerCode),
			cancellationToken).ConfigureAwait(false);

		return bucketId;
	}

	async ValueTask<BucketInfo?> IBucketStore.GetNotFullBucketAsync(CancellationToken cancellationToken)
	{
		if (_buckets.Count == 0)
			return null;

		var bucket = _buckets[0];

		if (bucket.IsFull is true || bucket.FillInAmount >= bucket.Amount)
		{
			if (!bucket.IsFull)
				await ((IBucketStore)this).FillInBucketAsync(
					bucket,
					new FillInBucketInfo(
						bucket.PlayerCode,
						0M,
						FillType.Fix,
						null,
						true),
					null,
					cancellationToken).ConfigureAwait(false);
			else
				await _eventSource.PublishBucketChangedAsync(
					new BucketChangedEvent(bucket.Id, PlayerCode),
					cancellationToken).ConfigureAwait(false);

			_buckets.RemoveAt(0);

			if (_buckets.Count == 0)
				return null;

			var snapshotBucket = await MakeSnapshotAsync(
				_buckets[0].Id,
				_buckets[0],
				cancellationToken).ConfigureAwait(false);

			if (snapshotBucket.SnapshotId != _buckets[0].SnapshotId)
			{
				_bucketAmount -= snapshotBucket.FillInAmount - _buckets[0].FillInAmount;
				_buckets[0] = snapshotBucket;
			}

			return await ((IBucketStore)this).GetNotFullBucketAsync(cancellationToken).ConfigureAwait(false);
		}

		return _buckets[0];
	}

	async ValueTask IBucketStore.FillInBucketAsync(
		BucketInfo bucket,
		FillInBucketInfo fillInInfo,
		string? remark,
		CancellationToken cancellationToken)
	{
		var newEventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		await _bucketEventRepository.FillInBucketAsync(
			newEventId,
			bucket.Id,
			fillInInfo,
			remark,
			cancellationToken).ConfigureAwait(false);

		var newBucket = fillInInfo.ApplyPatch(bucket) with
		{
			SnapshotId = newEventId
		};

		_buckets[0] = newBucket;

		_bucketAmount -= fillInInfo.Amount;

		await _eventSource.PublishBucketChangedAsync(
			new BucketChangedEvent(bucket.Id, PlayerCode),
			cancellationToken).ConfigureAwait(false);
	}

	ValueTask<ReadBucketResult> IBucketStore.GetBucketAmountAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		return ValueTask.FromResult(new ReadBucketResult(_bucketAmount, _buckets.LastOrDefault()?.Id));
	}

	async ValueTask IBucketStore.CleanUpBucketsAsync(
		Guid anchorBucketId,
		string? externalId,
		string? remark,
		CancellationToken cancellationToken)
	{
		await foreach (var bucket in IterateNotFullBucketsAsync(cancellationToken))
		{
			if (bucket.Id.CompareTo(anchorBucketId) > 0)
				break;

			var fillAmount = bucket.Amount - bucket.FillInAmount;

			var fillInfo = new FillInBucketInfo(
				PlayerCode,
				fillAmount,
				FillType.CleanUp,
				externalId,
				true);

			await ((IBucketStore)this).FillInBucketAsync(
				bucket,
				fillInfo,
				remark,
				cancellationToken).ConfigureAwait(false);
		}
	}

	public ValueTask DisposeAsync()
	{
		Dispose(true);
		GC.SuppressFinalize(this);

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
				_timer?.Dispose();
				_timer = null;

				Disposed?.Invoke(this, EventArgs.Empty);
				_channel.Writer.Complete();
				_cancellationTokenSource.Cancel();
			}

			_cancellationTokenSource.Dispose();
			_disposedValue = true;
		}
	}

	private async IAsyncEnumerable<BucketInfo> IterateNotFullBucketsAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		while (await ((IBucketStore)this)
			.GetNotFullBucketAsync(cancellationToken)
			.ConfigureAwait(false) is BucketInfo bucket)
			yield return bucket;
	}

	private async ValueTask ProcessMigrateAsync(CancellationToken cancellationToken = default)
	{
		var hasMigrate = await _tgsRepository.GetHasMigrateAsync(
			PlayerCode,
			cancellationToken).ConfigureAwait(false);

		if (hasMigrate)
			return;

		if (_buckets.Count == 0)
		{
			await _tgsRepository.SetHasMigrateAsync(
				PlayerCode,
				cancellationToken).ConfigureAwait(false);

			return;
		}

		for (var i = 0; i < _buckets.Count; i++)
		{
			var snapshotBucket = await MakeSnapshotAsync(
				_buckets[i].Id,
				_buckets[i],
				cancellationToken).ConfigureAwait(false);

			if (snapshotBucket.SnapshotId != _buckets[i].SnapshotId)
			{
				_bucketAmount -= snapshotBucket.FillInAmount - _buckets[i].FillInAmount;
				_buckets[i] = snapshotBucket;
			}

			await _tgsRepository.UpdateLegacyBucketFullAsync(
				snapshotBucket.Id,
				cancellationToken).ConfigureAwait(false);
		}

		var amount = _bucketAmount;

		_bucketAmount = 0M;
		_buckets.Clear();

		var createCommand = new CreateBucketCommand(amount, BucketType.Migrate, null, null);

		await PushCommandAsync(
			createCommand,
			cancellationToken).ConfigureAwait(false);

		await _tgsRepository.SetHasMigrateAsync(
			PlayerCode,
			cancellationToken).ConfigureAwait(false);
	}

	private async ValueTask PrepareNotFullBucketsAsync(CancellationToken cancellationToken = default)
	{
		var databaeAllNotFullBuckets = await _tgsRepository
			.GetPlayerNotFullBucketsAsync(PlayerCode, cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var tempAllBuckets = await _bucketEventRepository
			.GetPlayerCreatedBucketsAsync(PlayerCode, cancellationToken)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var tempBuckets = from info in tempAllBuckets
						  join bucket in databaeAllNotFullBuckets
							on info.Id equals bucket.Id into g
						  from bucket in g.DefaultIfEmpty()
						  where bucket is null
						  select info;

		await foreach (var bucket in databaeAllNotFullBuckets.Union(tempBuckets).ToAsyncEnumerable()
			.OrderBy(bucket => bucket.Id)
			.WithCancellation(cancellationToken)
			.ConfigureAwait(false))
		{
			_buckets.Add(bucket);
			_bucketAmount += bucket.Amount - bucket.FillInAmount;

			await _eventSource.PublishBucketChangedAsync(
				new BucketChangedEvent(bucket.Id, PlayerCode),
				cancellationToken).ConfigureAwait(false);
		}

		if (_buckets.Count > 0)
		{
			var snapshotBucket = await MakeSnapshotAsync(
				_buckets[0].Id,
				_buckets[0],
				cancellationToken).ConfigureAwait(false);

			if (snapshotBucket.SnapshotId != _buckets[0].SnapshotId)
			{
				_bucketAmount -= snapshotBucket.FillInAmount - _buckets[0].FillInAmount;
				_buckets[0] = snapshotBucket;
			}
		}
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

		var snapshotId = source?.SnapshotId;
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
}
