using Tgs.PlayerBucket.Clients;
using Tgs.Service.PlayerBuckets;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketClient(
		this IServiceCollection services,
		Action<IHttpClientBuilder> configureHttpClient)
	{
		configureHttpClient(
			services.AddHttpClient<IPlayerBucketClient, PlayerBucketClient>());

		return services;
	}
}
