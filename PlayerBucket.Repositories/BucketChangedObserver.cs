using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Tgs.MessageQueue;

namespace Tgs.Service.PlayerBuckets.Repositories;

internal class BucketChangedObserver : BackgroundService
{
	private readonly IMessageSender _messageSender;
	private readonly IDisposable _subscribe;

	public BucketChangedObserver(IEventBus eventBus, IMessageSender messageSender, ILogger<BucketChangedObserver> logger)
	{
		_subscribe = Observable.Using(
			() => Disposable.Empty,
			res =>
			{
				var flags = new ConcurrentDictionary<Guid, Guid>();

				return eventBus.BucketChangedObservable
					.Select(evt => (Evt: evt, Nonce: Guid.NewGuid()))
					.Where(tp => flags.GetOrAdd(tp.Evt.BucketId, key => tp.Nonce) == tp.Nonce)
					.Select(tp => tp.Evt)
					.Select(evt => Observable
						.FromAsync(async () =>
						{
							await Task.Yield();

							_ = flags.TryRemove(evt.BucketId, out _);

							return evt;
						})
						.Select(evt => Observable.FromAsync(async () =>
						{
							await Task.Yield();

							await PublishEventAsync(evt).ConfigureAwait(false);
						}))
						.Concat()
						.Catch((Exception ex) =>
						{
							logger.LogError(ex, "Publish event occur error!");

							return Observable.Empty<Unit>();
						}))
					.Concat();
			})
			.Subscribe();
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_ = stoppingToken.Register(_subscribe.Dispose);

		return Task.CompletedTask;
	}

	private async Task PublishEventAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default)
		=> await _messageSender.PublishAsync(
			"tgs.bucket.persistent",
			evt.ToBson(),
			cancellationToken).ConfigureAwait(false);
}
