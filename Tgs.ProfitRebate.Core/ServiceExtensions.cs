using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Tgs.ProfitRebate.Core;

public static class ServiceExtensions
{
	public static IServiceCollection AddBackOfficeApplicationServices(this IServiceCollection services)
		=> services
			.AddMediatR(Assembly.GetExecutingAssembly())
			.AddTransient(typeof(ITgsService<>), typeof(TgsService<>));
}
