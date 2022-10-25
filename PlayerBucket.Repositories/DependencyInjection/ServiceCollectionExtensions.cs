using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBuckets.DatabaseAccess;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddEventBus(this IServiceCollection services)
		=> services
			.AddHostedService<BucketChangedObserver>();

	public static IServiceCollection AddPlayerBucketRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbContextConfigure)
		=> services
			.AddDbContext<TgsContext>(dbContextConfigure)
			.AddScoped<PlayerBucketDataAccess>()
			.AddTransient<ITgsRepository, TgsRepository>();

	public static IServiceCollection AddPlayerBucketDistributedResourceManager(
		this IServiceCollection services,
		Func<IServiceProvider, StackExchange.Redis.IDatabase> resolveRedisDatabase)
		=> services
			.AddSingleton(sp => ActivatorUtilities.CreateInstance<RedisDistributedResourceManager>(
				sp,
				resolveRedisDatabase(sp)))
			.AddTransient<IDistributedResourceManager>(sp => sp.GetRequiredService<RedisDistributedResourceManager>())
			.AddTransient<IBucketEventRepository>(sp => sp.GetRequiredService<RedisDistributedResourceManager>());
}
