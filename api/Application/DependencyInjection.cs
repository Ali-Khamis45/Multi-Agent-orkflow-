using System.Reflection;
using AiAgentsTeam.Application.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace AiAgentsTeam.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddScoped<ISchedulerService, SchedulerService>();
        return services;
    }
}
