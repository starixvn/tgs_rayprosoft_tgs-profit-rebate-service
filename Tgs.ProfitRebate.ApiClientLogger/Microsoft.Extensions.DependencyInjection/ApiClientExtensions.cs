using Tgs.ProfitRebate.ApiClientLogger;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProfitRebateClientExtensions
    {
        public static IServiceCollection ApiClientExtensions(this IServiceCollection services)
        {
            services.AddScoped<IApiClientLogger, ApiClientLogger>();

            return services;
        }
    }
}
