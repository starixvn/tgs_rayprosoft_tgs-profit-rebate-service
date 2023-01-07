using System.Reflection;
using MediatR;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Workers;

namespace Microsoft.Extensions.DependencyInjection;

public static class ProfitRebateCoreExtensions
{
	public static IServiceCollection AddProfitRebateCore(this IServiceCollection services)
		=> services
			.AddMediatR(Assembly.GetExecutingAssembly())
			.AddTransient(typeof(ITgsService<>), typeof(TgsService<>))
			.AddTransient<IAgentDividendFactory, AgentDividendFactory>()
			.AddTransient<IPlayerRebateFactory, PlayerRebateFactory>()
            .AddTransient<IPlayerAllAgentRebateTaskQueueProcessor, PlayerAllAgentRebateTaskQueueProcessor>();
}
