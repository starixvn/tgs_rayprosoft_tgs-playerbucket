using System.Text.Json;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.Repositories.Entities;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Committer.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _dbContext;

	public TgsRepository(TgsContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async ValueTask InsertBucketEventsAsync(IEnumerable<BucketEvent> events, CancellationToken cancellationToken = default)
	{
		await _dbContext.PlayerBucketEventSources
			.AddRangeAsync(events.Select(evt => new PlayerBucketEventSource
			{
				Id = evt.Id,
				BucketId = evt.BucketId,
				ChangeAmount = evt.ChangeAmount,
				FillType = evt.Type.ToString(),
				ExternalId = evt.ExternalId,
				ChangeSets = JsonSerializer.Serialize(evt.ChangeSets),
				CreatedOn = evt.CreatedOn.ToUniversalTime()
			}), cancellationToken)
			.ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
