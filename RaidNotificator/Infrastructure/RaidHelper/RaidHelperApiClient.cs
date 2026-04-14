using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RaidNotificator.Contracts;
using RaidNotificator.DTOs;

namespace RaidNotificator.Infrastructure.RaidHelper;

public sealed class RaidHelperApiClient : IRaidHelperApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<RaidHelperApiClient> _logger;

    public RaidHelperApiClient(HttpClient http, ILogger<RaidHelperApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<RaidEvent?> GetEventByMessageIdAsync(ulong messageId, CancellationToken cancellationToken = default)
    {
        var path = $"events/{messageId}";
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var response = await _http.GetAsync(path, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Für manche Message-IDs normal -> kein Fehler
                    return null;
                }

                if (IsTransient(response.StatusCode))
                {
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(Backoff(attempt), cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Raid-Helper API transient error {StatusCode} for message {MessageId}.",
                        (int)response.StatusCode, messageId);
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Raid-Helper API non-success {StatusCode} for message {MessageId}.",
                        (int)response.StatusCode, messageId);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<RaidEvent>(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient timeout
                if (attempt < maxAttempts)
                {
                    await Task.Delay(Backoff(attempt), cancellationToken);
                    continue;
                }

                _logger.LogWarning("Raid-Helper API timeout for message {MessageId}.", messageId);
                return null;
            }
            catch (HttpRequestException ex)
            {
                if (attempt < maxAttempts)
                {
                    await Task.Delay(Backoff(attempt), cancellationToken);
                    continue;
                }

                _logger.LogWarning(ex, "Raid-Helper API request failed for message {MessageId}.", messageId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Raid-Helper API for message {MessageId}.", messageId);
                return null;
            }
        }

        return null;
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == (HttpStatusCode)429 ||
        (int)statusCode >= 500;

    private static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1));
}
