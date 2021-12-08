using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Repository;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TgsRepositoryExtensions
    {
        public static IServiceCollection AddTgsRepository(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<TgsRepository>(options =>
            {
                options.UseNpgsql(connectionString);
                options.UseSnakeCaseNamingConvention();
                options.UseLazyLoadingProxies();
            });

            services.AddScoped<ITgsRepository>(c => c.GetRequiredService<TgsRepository>());
            services.AddTransient<IAgentWalletRepository, AgentWalletRepository>();

            return services;
        }
    }
}