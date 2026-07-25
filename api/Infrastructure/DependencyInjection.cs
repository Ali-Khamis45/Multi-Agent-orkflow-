using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Infrastructure.AiRuntime;
using AiAgentsTeam.Infrastructure.Auth;
using AiAgentsTeam.Infrastructure.EventBus;
using AiAgentsTeam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace AiAgentsTeam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Redis.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<IEventBus, RedisStreamsEventBus>();
        services.AddHostedService<OrchestratorEventConsumer>();

        var aiRuntimeBaseUrl = configuration["AiRuntime:BaseUrl"]
            ?? throw new InvalidOperationException("Missing AiRuntime:BaseUrl.");

        services.AddHttpClient<IAiRuntimeClient, AiRuntimeClient>(client =>
        {
            client.BaseAddress = new Uri(aiRuntimeBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
