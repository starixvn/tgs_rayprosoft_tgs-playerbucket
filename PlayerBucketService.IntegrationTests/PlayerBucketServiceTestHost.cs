using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using Tgs.PlayerBuckets.DatabaseAccess.Entities;

namespace PlayerBucketService.IntegrationTests;

extern alias Server;

internal class PlayerBucketServiceTestHost
{
	public static WebApplicationFactory<Server::Program> Instance => new WebApplicationFactory<Server::Program>()
		.WithWebHostBuilder(hostBuilder => hostBuilder
			.ConfigureAppConfiguration(configBuilder => configBuilder.Sources.Clear())
			.ConfigureServices(services => services
				.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
					sp => new ConfigureNamedOptions<OtlpExporterOptions>(
						Options.DefaultName,
						exporterOptions => exporterOptions.TimeoutMilliseconds = 0))
				.Replace(ServiceDescriptor.Singleton(sp =>
				{
					var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
					_ = dbContextOptionsBuilder
						.UseInMemoryDatabase(Guid.NewGuid().ToString())
						.ConfigureWarnings(x => x.Ignore(
							InMemoryEventId.TransactionIgnoredWarning));

					return new TgsContext(dbContextOptionsBuilder.Options);
				}))));
}
