using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SmartToolbox.Services;

public sealed class RateLimiter
{
    private static readonly Lazy<RateLimiter> _instance = new(() => new RateLimiter());
    public static RateLimiter Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastRequestTimes = new();
    private readonly ConcurrentQueue<PendingRequest> _pendingQueue = new();
    private int _requestsPerMinute = 60;
    private int _minIntervalMs = 100;

    public int RequestsPerMinute
    {
        get => _requestsPerMinute;
        set => _requestsPerMinute = Math.Max(1, value);
    }

    public int MinIntervalMs
    {
        get => _minIntervalMs;
        set => _minIntervalMs = Math.Max(0, value);
    }

    private RateLimiter() { }

    public async Task<T> ExecuteAsync<T>(string provider, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphores.GetOrAdd(provider, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            await EnsureMinIntervalAsync(provider, cancellationToken);

            var result = await action();
            _lastRequestTimes[provider] = DateTime.UtcNow;
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task EnsureMinIntervalAsync(string provider, CancellationToken cancellationToken)
    {
        if (_lastRequestTimes.TryGetValue(provider, out var lastTime))
        {
            var elapsed = DateTime.UtcNow - lastTime;
            var waitTime = TimeSpan.FromMilliseconds(_minIntervalMs) - elapsed;

            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime, cancellationToken);
            }
        }
    }

    public void Configure(int requestsPerMinute, int minIntervalMs = 100)
    {
        RequestsPerMinute = requestsPerMinute;
        MinIntervalMs = minIntervalMs;
    }

    public TimeSpan? GetTimeUntilNextRequest(string provider)
    {
        if (_lastRequestTimes.TryGetValue(provider, out var lastTime))
        {
            var elapsed = DateTime.UtcNow - lastTime;
            var waitTime = TimeSpan.FromMilliseconds(_minIntervalMs) - elapsed;
            return waitTime > TimeSpan.Zero ? waitTime : null;
        }
        return null;
    }

    public void Reset(string provider)
    {
        _lastRequestTimes.TryRemove(provider, out _);
    }

    public void ResetAll()
    {
        _lastRequestTimes.Clear();
    }
}

internal class PendingRequest
{
    public string Provider { get; set; } = string.Empty;
    public TaskCompletionSource<bool> CompletionSource { get; set; } = new();
}
