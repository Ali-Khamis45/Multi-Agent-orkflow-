using System.Text.Json;
using AiAgentsTeam.Application.Common.Messaging;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AiAgentsTeam.Infrastructure.EventBus;

/// <summary>
/// Redis Streams implementation of the Event Bus (ARCHITECTURE.md §6.1). A single
/// stream ("workflow-events") carries every event type; consumer groups give
/// at-least-once delivery and replay-from-offset. This is the only class in the
/// whole solution that knows the transport is Redis — everything else depends on
/// <see cref="IEventBus"/>, so swapping transports later touches only this file.
/// </summary>
public sealed class RedisStreamsEventBus(IConnectionMultiplexer redis, ILogger<RedisStreamsEventBus> logger) : IEventBus
{
    public const string StreamKey = "workflow-events";

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var json = JsonSerializer.Serialize(envelope);
        await db.StreamAddAsync(StreamKey, "data", json);
        logger.LogDebug("Published event {Type} ({EventId})", envelope.Type, envelope.EventId);
    }

    public async Task SubscribeAsync(
        string consumerGroup,
        string consumerName,
        Func<EventEnvelope, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        await EnsureGroupAsync(db, consumerGroup);

        while (!cancellationToken.IsCancellationRequested)
        {
            StreamEntry[] entries;
            try
            {
                entries = await db.StreamReadGroupAsync(
                    StreamKey, consumerGroup, consumerName, ">", count: 10);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redis Streams read failed for group {Group}; retrying shortly.", consumerGroup);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
            }

            if (entries.Length == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                continue;
            }

            foreach (var entry in entries)
            {
                await ProcessEntryAsync(db, consumerGroup, entry, handler, cancellationToken);
            }
        }
    }

    private async Task ProcessEntryAsync(
        IDatabase db,
        string consumerGroup,
        StreamEntry entry,
        Func<EventEnvelope, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            string? json = entry.Values.FirstOrDefault(v => v.Name == "data").Value;
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(json!);
            if (envelope is not null)
                await handler(envelope, cancellationToken);

            await db.StreamAcknowledgeAsync(StreamKey, consumerGroup, entry.Id);
        }
        catch (Exception ex)
        {
            // §10 Self-Healing philosophy applied to the bus itself: a bad event never
            // crashes the consumer loop. It is left un-ACKed for later reprocessing/inspection.
            logger.LogError(ex, "Failed to process event {EntryId} in group {Group}", entry.Id, consumerGroup);
        }
    }

    private static async Task EnsureGroupAsync(IDatabase db, string consumerGroup)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(StreamKey, consumerGroup, StreamPosition.NewMessages, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists — expected on every restart after the first.
        }
    }
}
