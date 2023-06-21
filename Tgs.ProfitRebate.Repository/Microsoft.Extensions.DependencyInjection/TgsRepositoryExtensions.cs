using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Repository;

namespace Microsoft.Extensions.DependencyInjection;

public static class TgsRepositoryExtensions
{
    public static IServiceCollection AddTgsRepository(this IServiceCollection services, string connectionString)
        => services
            .AddDbContext<TgsRepository>(options =>
            {
                _ = options.UseNpgsql(connectionString);
                _ = options.UseSnakeCaseNamingConvention();
                _ = options.UseLazyLoadingProxies();
            })
            .AddScoped<ITgsRepository>(c => c.GetRequiredService<TgsRepository>())
            .AddTransient<IAgentWalletRepository, AgentWalletRepository>();
}