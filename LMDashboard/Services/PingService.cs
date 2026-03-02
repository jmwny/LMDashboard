using System.Diagnostics;

namespace LMDashboard.Services;

public class PingService(LinkStore store, IHttpClientFactory httpClientFactory, ILogger<PingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var links = store.Links;
            var now = DateTime.Now;

            foreach (var link in links)
            {
                if (link.IsPinging)
                    continue;

                var needsPing = link.LastChecked is null
                    || (now - link.LastChecked.Value).TotalSeconds >= link.PingIntervalSeconds;

                if (needsPing)
                {
                    store.SetPinging(link.Id);
                    _ = PingAsync(link.Id, link.Url, stoppingToken);
                }
            }
        }
    }

    private async Task PingAsync(Guid id, string url, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = httpClientFactory.CreateClient("Ping");
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            sw.Stop();
            store.UpdateStatus(id, (int)response.StatusCode, response.ReasonPhrase, sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            store.UpdateStatus(id, null, "TIMEOUT", sw.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Failed to ping {Url}", url);
            store.UpdateStatus(id, null, "UNREACHABLE", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            logger.LogError(ex, "Unexpected error pinging {Url}", url);
            store.UpdateStatus(id, null, "ERROR", sw.ElapsedMilliseconds);
        }
    }
}
