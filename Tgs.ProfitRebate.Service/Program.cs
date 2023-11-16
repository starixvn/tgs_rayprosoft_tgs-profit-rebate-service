using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Core.Workers;

AppContext.SetSwitch("Npgsql.EnableStoredProcedureCompatMode", true);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true)
    .AddEnvironmentVariables();

builder.Host.ConfigureServices((context, services) => services
    .AddHostedService<AgentDividendBackground>()
    .AddHostedService<PlayerRebateBackground>()
    .AddHostedService<PlayerAllAgentRebateBackground>()
    .AddHostedService<AgentDividendDistributionBackground>()
    .AddSingleton<IAgentDividendTaskQueue, AgentDividendTaskQueue>()
    .AddSingleton<IPlayerRebateTaskQueue, PlayerRebateTaskQueue>()
    .AddSingleton<IPlayerAllAgentRebateTaskQueue, PlayerAllAgentRebateTaskQueue>()
    .AddSingleton<IAgentDividendDistributionTaskQueue, AgentDividendDistributionTaskQueue>());

// Add services to the container.
builder.Services
    .AddTgsRepository(builder.Configuration.GetConnectionString("Tgs")!)
    .AddSystemJsonSerializer()
    .AddTgsLog()
    .AddProfitRebateCore()
    .AddCashServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("ProfitRebate:CashBaseUrl")!;
    })
    .AddPlayerBucketClient(httpClientBuilder => httpClientBuilder
        .ConfigureHttpClient(httpClient => httpClient.BaseAddress =
            builder.Configuration.GetValue<Uri>("ProfitRebate:PlayerBucketBaseUrl")))
    .AddPlayerServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("ProfitRebate:PlayerServiceBaseUrl")!;
    });

builder.Services
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((options, sp) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            options.Url = config.GetConnectionString("Nats")!;
        }))
    .AddRabbitMessageQueue(config => config
        .ConfigQueueOptions((options, sp) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();

            configuration.GetSection("RabbitMQ").Bind(options);
        }))
    .ClearAllExchanges()
    .AddRabbitGlobPatternExchange("report.error", "amq.direct")
    .AddNatsGlobPatternExchange("*");

builder.Services.AddNotificationClient();

builder.Services.AddHealthChecks();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
            fluentOptions.UseMessagePack();
        });
    });

builder.Services
    .AddOptions<OtlpExporterOptions>()
    .Configure<IConfiguration>((options, config) =>
    {
        var nodeIp = config.GetValue<string>("NODE_IP");

        if (!string.IsNullOrWhiteSpace(nodeIp))
        {
            options.Endpoint = new($"http://{nodeIp}:4317");
        }

        config.GetSection("Opentelemetry:Tracing").Bind(options);
    });

builder.Services
    .AddOpenTelemetry()
    .WithTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .WithMetrics()
    .Services
    .Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
        httpContext => !httpContext.Request.Path.HasValue
            || (!httpContext.Request.Path.StartsWithSegments("/swagger")
                && !httpContext.Request.Path.StartsWithSegments("/health")
                && !httpContext.Request.Path.StartsWithSegments("/profiler/")
                && !new[] { "html", "ico", "png", "jpg" }.Any(
                    ext => httpContext.Request.Path.Value.EndsWith($".{ext}"))));
builder.Services.AddProfileViewer();

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandler = async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error != null)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("ExceptionHandler");

            logger.LogError(exceptionFeature.Error, "Unhandle exception occur.");

            var messageSender = app.Services.GetRequiredService<IMessageSender>();

            await messageSender.PublishAsync("report.error",
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ReportErrorMessage(
                        $"{app.Environment}.profit.rebate: {exceptionFeature.Error.Message}"))))
                .ConfigureAwait(false);

            await context.Response.WriteAsJsonAsync(new MessageResponse
            {
                Message = exceptionFeature.Error.Message,
                Valid = false
            });
        }
    }
});

app.UseSwaggerUI();

app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/health");

app.Run();