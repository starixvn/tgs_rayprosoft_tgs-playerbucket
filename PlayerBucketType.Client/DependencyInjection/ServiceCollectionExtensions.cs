using Tgs.PlayerBucket.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketTypeClient(this IServiceCollection services)
		=> services
			.AddTransient<IPlayerBucketForTypesClient, PlayerBucketForTypesClient>();
}
