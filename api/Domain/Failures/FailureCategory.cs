namespace AiAgentsTeam.Domain.Failures;

public enum FailureCategory
{
    Validation,
    Transient,
    Permission,
    Provider,
    Timeout,
    Unknown
}
