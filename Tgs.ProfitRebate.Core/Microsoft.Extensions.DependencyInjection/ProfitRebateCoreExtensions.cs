using System.Reflection;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Workers;

namespace Microsoft.Extensions.DependencyInjection;

public static class ProfitRebateCoreExtensions
{
    public static IServiceCollection AddProfitRebateCore(this IServiceCollection services)
        => services
            .AddMediatR(r => r.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
            .AddTransient(typeof(ITgsService<>), typeof(TgsService<>))
            .AddTransient<IAgentDividendFactory, AgentDividendFactory>()
            .AddTransient<IPlayerRebateFactory, PlayerRebateFactory>()
            .AddTransient<IPlayerAllAgentRebateFactory, PlayerAllAgentRebateFactory>()
            .AddTransient<IPlayerAllAgentRebateTaskQueueProcessor, PlayerAllAgentRebateTaskQueueProcessor>();
}