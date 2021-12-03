using System.Reflection;
using MediatR;
using Tgs.ProfitRebate.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProfitRebateCoreExtensions
    {
        public static IServiceCollection AddProfitRebateCore(this IServiceCollection services)
        {
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(ITgsService<>), typeof(TgsService<>));
            services.AddTransient<IAgentDividendFactory, AgentDividendFactory>();

            return services;
        }
    }
}