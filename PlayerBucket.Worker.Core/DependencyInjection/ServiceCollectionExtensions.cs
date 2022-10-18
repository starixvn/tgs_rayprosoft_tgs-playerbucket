using System.Reflection;
using Tgs.Service.PlayerBuckets;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketWorker(this IServiceCollection services)
		=> services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
			.AddSerialNumberGenerator()
			.AddSingleton<IBucketDispatcher, BucketDispatcher>();
}
