using Tgs.ProfitRebate.Service.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((_, builder) =>
{
    builder.AddJsonFile("/config/appsettings.json", true);
});

// Add services to the container.
builder.Services
    .AddTgsRepository(builder.Configuration.GetConnectionString("Tgs"))
    .AddSystemJsonSerializer()
    .AddTgsLog()
    .ApiClientExtensions()
    .AddProfitRebateCore()
    .AddCashServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("ProfitRebate:CashBaseUrl");
    }); ;

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


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();
