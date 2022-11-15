using Microsoft.EntityFrameworkCore;
using Tgs.PlayerBuckets.DatabaseAccess;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketServiceRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbContextConfigure)
		=> services
			.AddDbContext<TgsContext>(dbContextConfigure)
			.AddScoped<PlayerBucketDataAccess>()
			.AddTransient<ITgsRepository, TgsRepository>();
}
