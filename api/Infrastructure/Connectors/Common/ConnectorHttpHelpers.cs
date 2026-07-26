using System.Text;
using System.Text.Json;

namespace AiAgentsTeam.Infrastructure.Connectors.Common;

/// <summary>Small shared HTTP/JSON conveniences used by every connector's HTTP calls —
/// plain utility, not connector-specific behavior, so sharing it doesn't violate "no
/// connector-specific logic in core" (nothing here knows what Shopify or GitHub is).</summary>
internal static class ConnectorHttpHelpers
{
    public static StringContent JsonBody(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    /// <summary>Deliberately does NOT catch exceptions — a transport-level failure
    /// (DNS, connection refused, timeout) must propagate to the caller's TryOrMock,
    /// which is the one place that decides "no path to the real service at all" should
    /// mock-fallback. A real HTTP response, even a 4xx/5xx one, returns normally here
    /// (HttpClient itself doesn't throw for non-2xx) so a genuinely-reachable service
    /// rejecting bad credentials surfaces as an honest failure, never a masked mock —
    /// confirmed live against Shopify's real API with a placeholder token.</summary>
    public static async Task<(bool Ok, string Body)> SendAsync(HttpClient http, HttpRequestMessage request, CancellationToken ct)
    {
        var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return (response.IsSuccessStatusCode, body);
    }

    public static string Truncate(string value, int max = 2000) => value.Length <= max ? value : value[..max] + "…";

    /// <summary>The connector equivalent of the AI Runtime's ModelRouter mock fallback:
    /// attempts the real call; if it throws for any reason (invalid/placeholder
    /// credentials, the provider being unreachable from this environment, a genuine API
    /// error), degrades to a deterministic, clearly-labeled mock result instead of a
    /// hard failure — same reason the whole platform runs end-to-end with zero external
    /// dependencies today. <paramref name="mock"/> receives the failure reason so the
    /// label is honest about *why* it's mocked, not just that it is.</summary>
    public static async Task<T> TryOrMock<T>(Func<Task<T>> real, Func<string, T> mock)
    {
        try
        {
            return await real();
        }
        catch (Exception ex)
        {
            return mock(Truncate(ex.Message, 200));
        }
    }
}
