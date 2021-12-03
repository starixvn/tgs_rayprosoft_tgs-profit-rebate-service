using MediatR;
using System.Reflection;
using Tgs.ProfitRebate.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProfitRebateCoreExtensions
    {
        public static IServiceCollection AddProfitRebateCore(this IServiceCollection services)
        {
            services.AddMediatR(Assembly.GetExecutingAssembly());
            services.AddTransient(typeof(ITgsService<>), typeof(TgsService<>));

            return services;
        }
    }
}
