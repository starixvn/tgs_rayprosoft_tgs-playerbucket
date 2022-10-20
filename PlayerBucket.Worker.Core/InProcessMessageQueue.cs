using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Tgs.Service.PlayerBuckets;

internal class InProcessMessageQueue : IEventBus, IEventSource
{
	private readonly Subject<BucketChangedEvent> _bucketChangedSubject = new();

	public IObservable<BucketChangedEvent> BucketChangedObservable => _bucketChangedSubject.AsObservable();

	public ValueTask PublishBucketChangedAsync(BucketChangedEvent evt, CancellationToken cancellationToken = default)
	{
		_bucketChangedSubject.OnNext(evt);

		return ValueTask.CompletedTask;
	}
}
