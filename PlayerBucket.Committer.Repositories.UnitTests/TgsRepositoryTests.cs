using System.Text.Json;
using ExpectedObjects;
using Json.More;
using Json.Patch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tgs.PlayerBuckets.Committer.Repositories;
using Tgs.PlayerBuckets.DatabaseAccess;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Tgs.Service.PlayerBuckets;

namespace PlayerBucket.Committer.Repositories.UnitTests;

[TestClass]
public class TgsRepositoryTests
{
	[TestMethod]
	public async Task 將BucketEvent存入資料庫()
	{
		var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(x => x.Ignore(
				InMemoryEventId.TransactionIgnoredWarning));
		var tgsContext = new TgsContext(dbContextOptionsBuilder.Options);
		var dataAccess = new PlayerBucketDataAccess(tgsContext);

		var sut = new TgsRepository(dataAccess, NullLogger<TgsRepository>.Instance);

		var bucketEvent = new BucketEvent(
			Guid.NewGuid(),
			Guid.NewGuid(),
			1000M,
			FillType.Deposit,
			Guid.NewGuid().ToString(),
			new JsonPatch().ToJsonDocument(),
			DateTime.UtcNow,
			"remark");

		var expected = new PlayerBucketEventSource
		{
			Id = bucketEvent.Id,
			BucketId = bucketEvent.BucketId,
			ChangeAmount = bucketEvent.ChangeAmount,
			ChangeSets = JsonSerializer.Serialize(bucketEvent.ChangeSets),
			CreatedOn = bucketEvent.CreatedOn,
			ExternalId = bucketEvent.ExternalId,
			FillType = bucketEvent.Type.ToString(),
			Remark = bucketEvent.Remark
		};

		await sut.InsertBucketEventsAsync([bucketEvent]);

		var rows = await tgsContext.PlayerBucketEventSources.ToArrayAsync();

		Assert.AreEqual(1, rows.Length);
		expected.ToExpectedObject().ShouldMatch(rows.First());
	}
}
