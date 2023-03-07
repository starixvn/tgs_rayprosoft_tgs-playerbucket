using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.MongoDB.Configuration;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("/config/appsettings.json", true);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
			fluentOptions.Host = nodeIp!;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
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
		builder.Configuration.GetConnectionString("Redis")!))
	.AddPlayerBucketCommitterRedisRepository(sp => sp.GetRequiredService<RedisConnectionManager>().Database);

builder.Services
	.AddMessageQueue()
	.AddMongoMessageQueue(config => config
		.HandleMongoMessageException((ex, _) =>
		{
			if (ex is OutOfMemoryException)
				OutOfMemoryExceptionHealthCheck.SetExceptionHappened();

			return Task.CompletedTask;
		})
		.DefineMessageQueue("PlayerBucket", sp => new MongoMessageQueueBuilderOptions
		{
			MessageQueue = MongoDB.Messaging.MessageQueue.Default,
			ConnectionString = builder.Configuration.GetConnectionString("Mongo")!
		})
		.DeclareQueue("bucket_event_requests")
		.AddHandler<ProcessBucketEventRequestDispatchHandler>("bucket_event_requests", TimeSpan.FromSeconds(10D)))
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, sp) =>
		{
			var configuration = sp.GetRequiredService<IConfiguration>();

			options.Url = configuration.GetConnectionString("Nats")!;
		})
		.AddProcessor<BidProcessor>("tgs.bucket.commit.bid")
		.AddHandler<ProcessBucketEventRequestHandler>($"tgs.bucket.commit.batch.{Environment.MachineName}")
		.AddHandler<SnapshotBucketHandler>($"tgs.bucket.commit.snapshot.{Environment.MachineName}")
		.AddHandler<SnapshotBucketDispatchHandler>("tgs.bucket.snapshot", "tgs.bucket.snapshot")
		.HandleNatsMessageException((ex, _) =>
		{
			if (ex is OutOfMemoryException)
				OutOfMemoryExceptionHealthCheck.SetExceptionHappened();

			return Task.CompletedTask;
		}))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddHealthChecks()
	.AddCheck<OutOfMemoryExceptionHealthCheck>("OOM", HealthStatus.Unhealthy);

builder.Services
	.AddOpenTelemetry()
	.WithTracing(tracingBuilder => tracingBuilder
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddNpgsql()
		.AddAspNetCoreInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterBuiler => meterBuiler
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
			.AddRuntimeInstrumentation()
			.AddAspNetCoreInstrumentation()
			.AddOtlpExporter())
	.Services
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
