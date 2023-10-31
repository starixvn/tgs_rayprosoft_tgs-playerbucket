using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;

namespace PlayerBucket.DatabaseAccess.UnitTests;

public class TgsContextTests
{
    [Fact]
    public async Task 復原變更後應該不會有資料變動_Insert()
    {
        var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
        _ = dbContextOptionsBuilder
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(
                InMemoryEventId.TransactionIgnoredWarning));

        var sut = new TgsContext(dbContextOptionsBuilder.Options);

        _ = await sut.PlayerBucketMigrations
            .AddAsync(new PlayerBucketMigration
            {
                PlayerCode = "test1"
            });

        sut.ChangeTracker.Clear();

        _ = await sut.SaveChangesAsync();

        Assert.Empty(sut.PlayerBucketMigrations);
    }

    [Fact]
    public async Task 復原變更後應該不會有資料變動_Update()
    {
        var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
        _ = dbContextOptionsBuilder
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(
                InMemoryEventId.TransactionIgnoredWarning));

        var sut = new TgsContext(dbContextOptionsBuilder.Options);

        var entity = new Tgs.PlayerBuckets.DatabaseAccess.Entities.PlayerBucket
        {
            Id = Guid.NewGuid(),
            BucketAmount = 100M,
            BucketType = 1,
            CurrentBucketAmount = 0M,
            IsFull = false,
            PlayerCode = "test1",
            SnapshotId = null,
            CreatedOn = DateTime.UtcNow
        };
        var entityEntry = await sut.PlayerBuckets
            .AddAsync(entity);

        _ = await sut.SaveChangesAsync();

        entity.CurrentBucketAmount = 10M;

        foreach (var entry in sut.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                case EntityState.Deleted:
                    entry.State = EntityState.Modified; //Revert changes made to deleted entity.
                    entry.State = EntityState.Unchanged;
                    break;

                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
            }
        }

        _ = await sut.SaveChangesAsync();

        Assert.Equal(0M, entity.CurrentBucketAmount);
    }
}
