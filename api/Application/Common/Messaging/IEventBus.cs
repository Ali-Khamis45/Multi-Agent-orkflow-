namespace AiAgentsTeam.Application.Common.Messaging;

/// <summary>
/// Transport-agnostic Event Bus contract (ARCHITECTURE.md §6). The Infrastructure
/// layer's Redis Streams implementation is the only thing that knows it's Redis —
/// callers in Application only ever see this interface (§6.1's stated design goal).
/// </summary>
public interface IEventBus
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>Starts a background consumer loop; handler runs per received event.</summary>
    Task SubscribeAsync(
        string consumerGroup,
        string consumerName,
        Func<EventEnvelope, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}
