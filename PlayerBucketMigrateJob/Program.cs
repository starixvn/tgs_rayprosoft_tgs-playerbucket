using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using PlayerBucketMigrateJob.Entities;
using Tgs.Service.PlayerBuckets;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddDbContext<TgsContext>(dbBuilder => dbBuilder
		.UseNpgsql("name=Tgs"))
	.AddPlayerBucketClient(httpBuilder => { })
	.AddKeyedSingleton(
		"Nats",
		(sp, _) => new NatsConnection(NatsOpts.Default with
		{
			Url = builder.Configuration.GetConnectionString("Nats")!
		}))
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats")))
	.AddNatsGlobPatternExchange("*");

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
