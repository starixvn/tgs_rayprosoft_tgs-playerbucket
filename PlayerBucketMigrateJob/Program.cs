using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlayerBucketMigrateJob.Entities;
using Tgs.PlayerBucket.Clients.DependencyInjection;
using Tgs.Service.PlayerBuckets;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services => services
	.AddDbContext<TgsContext>(dbBuilder => dbBuilder
		.UseNpgsql("name=Tgs"))
	.AddPlayerBucketClient(httpBuilder => { })
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, sp) =>
		{
			var configuration = sp.GetRequiredService<IConfiguration>();

			options.Url = configuration.GetConnectionString("Nats")!;
		}))
		.AddNatsGlobPatternExchange("*"));

var app = builder.Build();

var dbContext = app.Services.GetRequiredService<TgsContext>();

var notMigratePlayers = from player in dbContext.Players
						join migrateRow in dbContext.PlayerBucketMigrations
							on player.PlayerCode equals migrateRow.PlayerCode into g
						from migrateRow in g.DefaultIfEmpty()
						where migrateRow.PlayerCode == null
						select player.PlayerCode;

var playerBucketClient = app.Services.GetRequiredService<IPlayerBucketClient>();

foreach (var playerCode in notMigratePlayers)
{
	_ = await playerBucketClient
		.ReadBucketsAsync(playerCode)
		.ConfigureAwait(false);
}
