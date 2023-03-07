using System.Text.Json.Serialization;
using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.PlayerBucket.Clients.DependencyInjection;

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
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers()
	.AddMvcOptions(mvcOptions => mvcOptions.AddWebApiToolkitFilters())
	.AddJsonOptions(options => options
		.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
	.AddPlayerBucketService()
	.AddPlayerBucketServiceRepositories((_, dbBuilder) => dbBuilder
		.UseNpgsql("name=Tgs"))
	.AddPlayerBucketClient(clientBuilder => { });

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(config => config
		.ConfigQueueOptions((options, sp) =>
		{
			var configuration = sp.GetRequiredService<IConfiguration>();

			options.Url = configuration.GetConnectionString("Nats")!;
		}))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddSwaggerGen(c =>
	{
		c.ApplyWebApiToolkitSupport();

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

var app = builder.Build();

app.MapControllers();
app.MapSwagger();
app.MapProfileViewer();
app.MapHealthChecks("/healthz");

app.UseSwaggerUI();

app.Run();
