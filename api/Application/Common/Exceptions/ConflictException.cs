namespace AiAgentsTeam.Application.Common.Exceptions;

/// <summary>A request conflicts with existing state (e.g. an email already
/// registered) — mapped to 409 Conflict by GlobalExceptionMiddleware.</summary>
public sealed class ConflictException(string message) : Exception(message);
