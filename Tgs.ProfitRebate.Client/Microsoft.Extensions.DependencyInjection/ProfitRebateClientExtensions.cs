using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tgs.ProfitRebate.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProfitRebateClientExtensions
    {
        public static IServiceCollection AddProfitRebateClient(this IServiceCollection services,
            IConfiguration configuration, string configSectionName = "ProfitRebate")
        {
            services.Configure<ClientSettings>(configuration.GetSection(configSectionName));

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClientSettings>>().Value);

            services.AddHttpClient<IProfitRebateClient, ProfitRebateClient>((sp, c) =>
            {
                var settings = sp.GetRequiredService<ClientSettings>();
                c.BaseAddress = new(settings.Url);
            });

            return services;
        }
    }
}