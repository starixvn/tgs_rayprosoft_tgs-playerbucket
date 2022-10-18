using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;

namespace Tgs.PlayerBucketWorker.IntegrationTests;

extern alias Server;

internal static class PlayerBucketWorkerTestHost
{
	public static WebApplicationFactory<Server::Program> Instance => new WebApplicationFactory<Server::Program>()
		.WithWebHostBuilder(hostBuilder => hostBuilder
			.ConfigureServices(services => services
				.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
					sp => new ConfigureNamedOptions<OtlpExporterOptions>(
						Options.DefaultName,
						exporterOptions => exporterOptions.TimeoutMilliseconds = 0))));
}
