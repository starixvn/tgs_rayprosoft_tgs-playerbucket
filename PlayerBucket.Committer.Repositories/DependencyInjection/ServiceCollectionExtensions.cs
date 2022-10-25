using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.Repositories;
using Tgs.PlayerBuckets.DatabaseAccess;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketCommitterRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbContextConfigure)
		=> services
			.AddDbContext<TgsContext>(dbContextConfigure)
			.AddScoped<PlayerBucketDataAccess>()
			.AddTransient<ITgsRepository, TgsRepository>();

	public static IServiceCollection AddPlayerBucketCommitterRedisRepository(
		this IServiceCollection services,
		Func<IServiceProvider, StackExchange.Redis.IDatabase> resolveRedisDatabase)
		=> services
			.AddTransient(sp => ActivatorUtilities.CreateInstance<RedisRepository>(
				sp,
				resolveRedisDatabase(sp)))
			.AddTransient<IBucketEventRepository>(sp => sp.GetRequiredService<RedisRepository>())
			.AddTransient<IDistributedResourceManager>(sp => sp.GetRequiredService<RedisRepository>());
}
