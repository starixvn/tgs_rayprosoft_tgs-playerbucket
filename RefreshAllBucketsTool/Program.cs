using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RefreshAllBucketsTool;
using Tgs.PlayerBuckets.DatabaseAccess;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
	.AddUserSecrets("4acd7ba9-2bfe-42ea-8e0e-83ffb920ec62");

builder.Services
	.AddSingleton(sp => ActivatorUtilities.CreateInstance<RedisConnectionManager>(
		sp,
		builder.Configuration.GetConnectionString("Redis")!))
	.AddDbContext<TgsContext>(dbBuilder => dbBuilder
		.UseNpgsql("name=Tgs"))
	.AddScoped<PlayerBucketDataAccess>()
	.AddSingleton(sp => new HttpClient
	{
		BaseAddress = builder.Configuration.GetValue<Uri>("Tool:WorkerBaseUrl")
	});
;

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var redisConnMgr = app.Services.GetRequiredService<RedisConnectionManager>();
var dataAccess = app.Services.GetRequiredService<PlayerBucketDataAccess>();
var httpClient = app.Services.GetRequiredService<HttpClient>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("AllCreateBuckets");

await foreach (var info in redisConnMgr.GetAllCreateBucketsAsync())
{
	_ = await httpClient.PutAsync(
		$"/api/bucket/flush/{info.PlayerCode}/{info.Id}",
		null).ConfigureAwait(false);
	logger.LogInformation("Process {playerCode}, {id}", info.PlayerCode, info.Id);
}

logger.LogInformation("AllBucketEvents");
var allBucketIds = await redisConnMgr.GetAllBucketIdsAsync()
	.ToArrayAsync()
	.ConfigureAwait(false);
var processBuckets = await dataAccess.QueryPlayerBucketsForFlushAsync(
	allBucketIds).ToArrayAsync().ConfigureAwait(false);

foreach (var (id, playerCode) in processBuckets)
{
	_ = await httpClient.PutAsync(
		$"/api/bucket/flush/{playerCode}/{id}",
		null).ConfigureAwait(false);
	logger.LogInformation("Process {playerCode}, {id}", playerCode, id);
}

var notHandleQuery = from id in allBucketIds
					 join t in processBuckets
						on id equals t.Id into g
					 from t in g.DefaultIfEmpty()
					 where id != t.Id
					 select id;

logger.LogInformation("Can't process ids");
foreach (var id in notHandleQuery)
	logger.LogWarning("Id: {Id}", id);
