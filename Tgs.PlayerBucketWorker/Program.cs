using System.Text.Json.Serialization;
using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.MongoDB.Configuration;
using Tgs.PlayerBucket.Clients.DependencyInjection;
using Tgs.PlayerBucketWorker;
using Tgs.PlayerBucketWorker.MessageQueue;
using Tgs.Service.PlayerBuckets;

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
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
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
	.AddPlayerBucketWorker()
	.AddEventBus()
	.AddPlayerBucketRepositories((_, contextBuilder) => contextBuilder
		.UseNpgsql("name=Tgs"))
	.AddSingleton(sp => ActivatorUtilities.CreateInstance<RedisConnectionManager>(
		sp,
		builder.Configuration.GetConnectionString("Redis")!))
	.AddPlayerBucketDistributedResourceManager(sp => sp.GetRequiredService<RedisConnectionManager>().Database)
	.AddPlayerBucketClient(httpClientBuilder => httpClientBuilder
		.ConfigureHttpClient(http => http.BaseAddress =
			builder.Configuration.GetValue<Uri>("PlayerBucket:ServiceBaseUrl")));

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, sp) =>
		{
			var configuration = sp.GetRequiredService<IConfiguration>();

			options.Url = configuration.GetConnectionString("Nats")!;
		})
		.AddProcessor<BidProcessor>("tgs.bucket.bid")
		.AddProcessor<CreateBucketProcessor>($"tgs.bucket.worker.create.{Environment.MachineName}")
		.AddReplyHandler<FillInBucketHandler>($"tgs.bucket.worker.fill.{Environment.MachineName}")
		.AddProcessor<GetBucketAmountProcessor>($"tgs.bucket.worker.amount.{Environment.MachineName}")
		.AddHandler<ExtendExpiryHandler>($"tgs.bucket.worker.expiry.{Environment.MachineName}")
		.AddReplyHandler<CleanBucketHandler>($"tgs.bucket.worker.clean.{Environment.MachineName}")
		.AddProcessor<CreateBucketDispatchProcessor>("tgs.bucket.create", "tgs.bucket.create")
		.AddReplyHandler<FillInBucketDispatchHandler>("tgs.bucket.fill", "tgs.bucket.fill")
		.AddProcessor<GetBucketAmountDispatchProcessor>("tgs.bucket.amount", "tgs.bucket.amount")
		.AddReplyHandler<CleanBucketDispatchHandler>("tgs.bucket.clean", "tgs.bucket.clean"))
	.AddMongoMessageQueue(config => config
		.DefineMessageQueue("PlayerBucket", sp => new MongoMessageQueueBuilderOptions
		{
			MessageQueue = MongoDB.Messaging.MessageQueue.Default,
			ConnectionString = builder.Configuration.GetConnectionString("Mongo")!
		})
		.DeclareQueue("bucket_event_requests"))
	.AddMongoGlobPatternExchange("tgs.bucket.persistent", "PlayerBucket", "bucket_event_requests")
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers()
	.AddJsonOptions(options => options
		.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
	.AddSwaggerGen(c =>
	{
		var xmlFileNames = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml");
		foreach (var xmlFileName in xmlFileNames)
			c.IncludeXmlComments(xmlFileName);
	});

builder.Services
	.AddHealthChecks();

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
			.AddMeter("Tgs.PlayerBucket")
			.AddInstrumentation<PlayerBucketMetrics>()
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

app.MapControllers();
app.MapSwagger();
app.MapProfileViewer();
app.MapHealthChecks("/healthz");

app.UseSwaggerUI();

app.Run();
