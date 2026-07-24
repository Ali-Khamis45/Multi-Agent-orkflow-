using AiAgentsTeam.Application;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Application.Scheduling;
using AiAgentsTeam.Infrastructure.EventBus;
using AiAgentsTeam.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace AiAgentsTeam.IntegrationTests;

/// <summary>
/// Real Postgres + Redis via Testcontainers — no mocking of persistence or the
/// Event Bus (Phase 1.5 §11 Integration Tests: "these tests should run
/// automatically" against the real infrastructure this system actually uses).
/// One fixture, shared across the collection, spins the containers up once.
/// </summary>
public sealed class PipelineTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("aiagentsteam_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7").Build();

    public ServiceProvider Services { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(_redis.GetConnectionString()));
        services.AddSingleton<IEventBus, RedisStreamsEventBus>();
        services.Configure<SchedulerOptions>(o => o.MaxTaskAttempts = 2);

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    public IServiceScope NewScope() => Services.CreateScope();
}

[CollectionDefinition("Pipeline")]
public sealed class PipelineCollection : ICollectionFixture<PipelineTestFixture>;
