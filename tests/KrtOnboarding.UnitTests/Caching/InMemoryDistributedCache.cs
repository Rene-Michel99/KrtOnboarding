using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace KrtOnboarding.UnitTests.Caching;

public class InMemoryDistributedCache : IDistributedCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

    public DistributedCacheEntryOptions? LastSetOptions { get; private set; }

    public byte[]? Get(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return null;

        if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return null;
        }

        return entry.Value;
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        Task.FromResult(Get(key));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        LastSetOptions = options;
        var expiresAt = options.AbsoluteExpirationRelativeToNow.HasValue
            ? DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value)
            : options.AbsoluteExpiration;
        _entries[key] = new CacheEntry(value, expiresAt);
    }

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
    }

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        Task.CompletedTask;

    public void Remove(string key) => _entries.TryRemove(key, out _);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    private sealed record CacheEntry(byte[] Value, DateTimeOffset? ExpiresAt);
}