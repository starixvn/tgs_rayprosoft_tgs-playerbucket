using System.Reflection;
using Tgs.Service.PlayerBuckets;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerBucketWorker(this IServiceCollection services)
	{
		PlayerBucketMeters.Init();

		return services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
			.AddSerialNumberGenerator()
			.AddSingleton<IBucketDispatcher, BucketDispatcher>()
			.AddSingleton<InProcessMessageQueue>()
			.AddTransient<IEventSource>(sp => sp.GetRequiredService<InProcessMessageQueue>())
			.AddTransient<IEventBus>(sp => sp.GetRequiredService<InProcessMessageQueue>());
	}
}
