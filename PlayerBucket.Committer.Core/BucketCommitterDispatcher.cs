using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.PlayerBuckets.Committer.ApplicationServices;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.PlayerBuckets.Committer;

internal class BucketCommitterDispatcher : BackgroundService
{
	private readonly IEventBus _eventBus;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<BucketCommitterDispatcher> _logger;

	public BucketCommitterDispatcher(IEventBus eventBus, IServiceProvider serviceProvider, ILogger<BucketCommitterDispatcher> logger)
	{
		_eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var processBatchSub = Observable.Using(
			() => Disposable.Empty,
			res =>
			{
				var flags = new ConcurrentDictionary<Guid, Guid>();

				return _eventBus.BucketChangedObservable
					.GroupByUntil(
						evt => evt.BucketId,
						g =>
						{
							var waitSeconds = 300;
							var trigger = new TaskCompletionSource();

							return Observable.Using(
								() => g.Subscribe(_ => Interlocked.Exchange(ref waitSeconds, 300)),
								d => Observable.Using(
									() => new Timer(
										_ =>
										{
											_ = Interlocked.Decrement(ref waitSeconds);
											if (waitSeconds <= 0)
												trigger.SetResult();
										}),
									tm => Observable.FromAsync(() => trigger.Task)));
						})
					.Select(g => g
						.Select(evt => (Evt: evt, Nonce: Guid.NewGuid()))
						.Where(tp => flags.GetOrAdd(tp.Evt.BucketId, key => tp.Nonce) == tp.Nonce)
						.Select(tp => tp.Evt)
						.Select(evt => Observable
							.FromAsync(async () =>
							{
								await Task.CompletedTask.ConfigureAwait(false);

								_ = flags.TryRemove(evt.BucketId, out _);

								return evt;
							})
							.Select(evt => Observable
								.FromAsync(async () =>
								{
									await Task.CompletedTask.ConfigureAwait(false);

									await using var scope = _serviceProvider.CreateAsyncScope();

									var serviceCoordinator = scope.ServiceProvider.GetRequiredService<IServiceCoordinator>();

									var command = new ProcessBucketEventsCommand(evt.BucketId, evt.PlayerCode);

									await serviceCoordinator.ExecuteAsync(command, stoppingToken).ConfigureAwait(false);
								})
								.Catch((Exception ex) =>
								{
									_logger.LogError(ex, "Process {evt.BucketId} event occur error!", evt.BucketId);

									return Observable.Empty<Unit>();
								}))
							.Concat())
						.Concat())
					.Merge();
			})
			.Subscribe();

		_ = stoppingToken.Register(() => processBatchSub.Dispose());

		var snapshotSub = Observable.Using(
			() => Disposable.Empty,
			res =>
			{
				var flags = new ConcurrentDictionary<Guid, Guid>();

				return _eventBus.SnapshotObservable
					.GroupByUntil(
						evt => evt.BucketId,
						g =>
						{
							var waitSeconds = 300;
							var trigger = new TaskCompletionSource();

							return Observable.Using(
								() => g.Subscribe(_ => Interlocked.Exchange(ref waitSeconds, 300)),
								d => Observable.Using(
									() => new Timer(
										_ =>
										{
											_ = Interlocked.Decrement(ref waitSeconds);
											if (waitSeconds <= 0)
												trigger.SetResult();
										}),
									tm => Observable.FromAsync(() => trigger.Task)));
						})
					.Select(g => g
						.Select(evt => (Evt: evt, Nonce: Guid.NewGuid()))
						.Where(tp => flags.GetOrAdd(tp.Evt.BucketId, key => tp.Nonce) == tp.Nonce)
						.Select(tp => tp.Evt)
						.Select(evt => Observable
							.FromAsync(async () =>
							{
								await Task.CompletedTask.ConfigureAwait(false);

								_ = flags.TryRemove(evt.BucketId, out _);

								return evt;
							})
							.Select(evt => Observable
								.FromAsync(async () =>
								{
									await Task.CompletedTask.ConfigureAwait(false);

									await using var scope = _serviceProvider.CreateAsyncScope();

									var serviceCoordinator = scope.ServiceProvider.GetRequiredService<IServiceCoordinator>();

									var command = new SnapshotPlayerBucketCommand(evt.BucketId, evt.PlayerCode);

									await serviceCoordinator.ExecuteAsync(command, stoppingToken).ConfigureAwait(false);
								})
								.Catch((Exception ex) =>
								{
									_logger.LogError(ex, "Process {evt.BucketId} event occur error!", evt.BucketId);

									return Observable.Empty<Unit>();
								}))
							.Concat())
						.Concat())
					.Merge();
			})
			.Subscribe();

		_ = stoppingToken.Register(() => snapshotSub.Dispose());

		return Task.CompletedTask;
	}
}
