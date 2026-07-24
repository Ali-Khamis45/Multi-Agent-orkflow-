namespace AiAgentsTeam.Application.Scheduling;

/// <summary>
/// Environment-tunable Scheduler/Self-Healing policy (Phase 1.5 Configuration
/// Layer hardening — was a hardcoded `const int MaxAttempts = 2` in
/// FailTaskCommandHandler). Bound from the "Scheduler" configuration section.
/// </summary>
public sealed class SchedulerOptions
{
    public const string SectionName = "Scheduler";

    public int MaxTaskAttempts { get; set; } = 2;
}
