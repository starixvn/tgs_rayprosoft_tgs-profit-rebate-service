var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddSystemJsonSerializer()
    .AddProfitRebateClient(builder.Configuration);

builder.Services
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((options, sp) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            options.Url = config.GetValue<string>("NatsSetting:Urls");
        }))
    .ClearAllExchanges()
    .AddNatsGlobPatternExchange("*");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();