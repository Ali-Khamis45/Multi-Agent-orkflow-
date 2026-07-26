using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Application.Connectors.Common;
using AiAgentsTeam.Infrastructure.AiRuntime;
using AiAgentsTeam.Infrastructure.Auth;
using AiAgentsTeam.Infrastructure.Connectors.Common;
using AiAgentsTeam.Infrastructure.Connectors.Founder;
using AiAgentsTeam.Infrastructure.Connectors.Software;
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

        AddConnectorFramework(services);

        return services;
    }

    /// <summary>Phase 4 "Connector Framework": every connector is a plain DI
    /// registration of IConnectorDefinition — this method is the *only* place in the
    /// whole codebase that has to know all 18 connectors exist. Adding #19 is one more
    /// line here plus the class itself; nothing else in Application/Api changes.</summary>
    private static void AddConnectorFramework(IServiceCollection services)
    {
        services.AddSingleton<ICredentialProtector, CredentialProtector>();
        services.AddSingleton<IConnectorOAuthStateSigner, ConnectorOAuthStateSigner>();
        services.AddSingleton<IConnectorConfig, ConnectorConfig>();
        services.AddScoped<ConnectorCredentialLoader>();
        services.AddHttpClient<IOAuth2TokenExchanger, OAuth2TokenExchanger>();
        // Scoped, not Singleton: AddHttpClient<TClient> below registers each connector as
        // Transient, and a Singleton registry holding them forever would be a captive-
        // dependency anti-pattern (stale HttpClient/DNS). Scoped means one fresh resolve
        // per request, same lifetime MediatR handlers already run at.
        services.AddScoped<IConnectorRegistry, ConnectorRegistry>();

        // Founder connectors
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.ShopifyConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.WooCommerceConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.StripeConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.MetaConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.GoogleAnalyticsConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.GoogleAdsConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.GmailConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.GoogleDriveConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Founder.NotionConnector>();

        // Software connectors
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.GitHubConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.GitLabConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.JiraConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.LinearConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.SlackConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.DiscordConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.DockerHubConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.VercelConnector>();
        services.AddHttpClient<IConnectorDefinition, Connectors.Software.AzureDevOpsConnector>();
    }
}
