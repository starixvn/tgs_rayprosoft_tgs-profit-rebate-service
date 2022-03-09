using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Service.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((_, builder) =>
{
    builder.AddJsonFile("/config/appsettings.json", true);
}).ConfigureServices((context, services) =>
{
    services.AddHostedService<AgentDividendBackground>();
    services.AddSingleton<IAgentDividendTaskQueue>(_ =>
    {
        if (!int.TryParse(context.Configuration["QueueCapacity"], out var queueCapacity))
        {
            queueCapacity = 100;
        }

        return new AgentDividendTaskQueue(queueCapacity);
    });
});

// Add services to the container.
builder.Services
    .AddTgsRepository(builder.Configuration.GetConnectionString("Tgs"))
    .AddSystemJsonSerializer()
    .AddTgsLog()
    .AddProfitRebateCore()
    .AddCashServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("ProfitRebate:CashBaseUrl");
    });

builder.Services
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig =>
    {
        natsConfig
            .ConfigQueueOptions((options, sp) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();

                options.Url = config.GetValue<string>("NatsSetting:Urls");
            });
    })
    .AddRabbitMessageQueue(config =>
    {
        config
            .ConfigQueueOptions((options, sp) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();

                configuration.GetSection("RabbitMQ").Bind(options);
            });
    })
    .ClearAllExchanges()
    .AddRabbitGlobPatternExchange("report.error", "amq.direct")
    .AddNatsGlobPatternExchange("*");

builder.Services.AddHealthChecks();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
            fluentOptions.UseMessagePack();
        });
    });

builder.Services
    .AddOptions<OtlpExporterOptions>()
    .Configure<IConfiguration>((options, conifg) =>
    {
        var nodeIp = conifg.GetValue<string>("NODE_IP");

        if (!string.IsNullOrWhiteSpace(nodeIp))
            options.Endpoint = new Uri($"http://{nodeIp}:4317");

        conifg.GetSection("Opentelemetry:Tracing").Bind(options);
    });

builder.Services
    .AddOpenTelemetryTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
        })
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .Configure<AspNetCoreInstrumentationOptions>(options =>
    {
        options.Filter = httpContext =>
        {
            return !httpContext.Request.Path.HasValue
                || !httpContext.Request.Path.StartsWithSegments("/swagger")
                && !httpContext.Request.Path.StartsWithSegments("/health")
                && !httpContext.Request.Path.StartsWithSegments("/profiler/")
                && !new[]
                {
                    "html",
                    "ico",
                    "png",
                    "jpg"
                }.Any(
                    ext => httpContext.Request.Path.Value.EndsWith($".{ext}"));
        };
    });
builder.Services.AddProfileViewer();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();