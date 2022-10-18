using Microsoft.Extensions.DependencyInjection;

namespace Tgs.PlayerBucket.Clients.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketClient(this IServiceCollection services)
		=> services
			.AddTransient<IPlayerBucketClient, PlayerBucketClient>();
}
