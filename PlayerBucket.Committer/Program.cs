using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NATS.Client.Core;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.PlayerBuckets.Committer;
using Tgs.PlayerBuckets.Committer.MessageQueue;
using Valhalla.MessageQueue.MongoDB.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry();

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("Otel:NodeIp");
			fluentOptions.Host = nodeIp!;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("Otel:ServiceName")!;
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
	.AddKeyedSingleton(
		"Nats",
		(sp, _) => new NatsConnection(NatsOpts.Default with
		{
			Url = builder.Configuration.GetConnectionString("Nats")!
		}))
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
		.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
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

var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(_ => _.AddService(builder.Configuration.GetValue<string>("Otel:ServiceName")))
	.WithTracing(tracingBuilder => tracingBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
		.AddNpgsql()
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/healthz")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !excludeExtensions.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddProfileViewExporter()
		.AddOtlpExporter(options => options.Endpoint = builder.Configuration.GetValue<Uri>("Otel:Collector")))
	.WithMetrics(meterBuiler => meterBuiler
			.AddRuntimeInstrumentation()
			.AddAspNetCoreInstrumentation()
			.AddOtlpExporter())
	.Services
	.AddProfileViewer();

builder.Services
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

var app = builder.Build();

app.MapProfileViewer();
app.MapHealthChecks("/healthz");

SentrySdk.CaptureMessage($"Started result PlayerBucketCommitter service at {DateTime.Now.ToString()}");

app.Run();
