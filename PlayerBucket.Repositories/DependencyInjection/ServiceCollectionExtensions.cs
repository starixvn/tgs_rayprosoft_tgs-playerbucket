using Microsoft.EntityFrameworkCore;
using Tgs.Service.PlayerBuckets;
using Tgs.Service.PlayerBuckets.Repositories;
using Tgs.Service.PlayerBuckets.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbContextConfigure)
		=> services
			.AddDbContext<TgsContext>(dbContextConfigure)
			.AddTransient<ITgsRepository, TgsRepository>();
}
