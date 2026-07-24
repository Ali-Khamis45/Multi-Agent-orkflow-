using System.Reflection;
using AiAgentsTeam.Application.Common.Behaviours;
using AiAgentsTeam.Application.Scheduling;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AiAgentsTeam.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // API Contract Validation (Phase 1.5 §10): every FluentValidation validator
        // in this assembly is auto-discovered and run by ValidationBehavior above.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<ISchedulerService, SchedulerService>();
        return services;
    }
}
