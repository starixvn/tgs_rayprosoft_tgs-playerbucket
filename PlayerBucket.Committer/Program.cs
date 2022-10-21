using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.MongoDB.Configuration;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
			fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
				"MachineInfo",
				() => new
				{
					Environment.MachineName
				}));
		});
	});

builder.Services
	.AddPlayerBucketCommitter()
	.AddPlayerBucketCommitterRepositories((_, dbBuilder) => dbBuilder
		.UseNpgsql("name=Tgs"))
	.AddSingleton(sp => ActivatorUtilities.CreateInstance<RedisConnectionManager>(
		sp,
		builder.Configuration.GetConnectionString("Redis")))
	.AddPlayerBucketCommitterRedisRepository(sp => sp.GetRequiredService<RedisConnectionManager>().Database);

builder.Services
	.AddMessageQueue()
	.AddMongoMessageQueue(config => config
		.DefineMessageQueue("PlayerBucket", sp => new MongoMessageQueueBuilderOptions
		{
			MessageQueue = MongoDB.Messaging.MessageQueue.Default,
			ConnectionString = builder.Configuration.GetConnectionString("Mongo")
		})
		.DeclareQueue("bucket_event_requests")
		.AddHandler<ProcessBucketEventRequestHandler>("bucket_event_requests", TimeSpan.FromSeconds(10D)));

builder.Services
	.AddHealthChecks();

builder.Services
	.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
		sp => new ConfigureNamedOptions<OtlpExporterOptions>(
			Microsoft.Extensions.Options.Options.DefaultName,
			exporterOptions =>
			{
				var config = sp.GetRequiredService<IConfiguration>();
				var nodeIp = config.GetValue<string>("NODE_IP");

				if (!string.IsNullOrWhiteSpace(nodeIp))
					exporterOptions.Endpoint = new Uri($"http://{nodeIp}:4317");

				config.GetSection("Opentelemetry:Tracing").Bind(exporterOptions);
			}))
	.AddOpenTelemetryTracing(
		tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddNpgsql()
			.AddAspNetCoreInstrumentation()
			.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
			.AddProfileViewExporter()
			.AddOtlpExporter())
	.AddProfileViewer()
	.Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
		httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/healthz")
			&& !httpContext.Request.Path.StartsWithSegments("/metrics")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
			&& !new[] { "html", "ico", "png", "jpg" }.Any(
				ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

builder.Services
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

var app = builder.Build();

app.MapProfileViewer();
app.MapHealthChecks("/healthz");

app.Run();
