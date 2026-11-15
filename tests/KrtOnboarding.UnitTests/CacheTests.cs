using System.Collections.Concurrent;
using System.Text;
using KrtOnboarding.Domain.Entities;
using KrtOnboarding.Domain.Enums;
using KrtOnboarding.Domain.ValueObjects;
using KrtOnboarding.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace KrtOnboarding.UnitTests;

public class CacheTests
{
    [Fact]
    public async Task SetAsync_Account_CanBeRetrievedWithCpfAndStatus()
    {
        var distributedCache = new InMemoryDistributedCache();
        var cache = new RedisCaching(distributedCache);
        var account = new Account
        {
            Id = 42,
            Name = "Ana",
            CPF = Cpf.Create("52998224725"),
            Status = AccountStatus.Active
        };

        await cache.SetAsync("account:42", account);
        var result = await cache.GetAsync<Account>("account:42");

        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal(account.Name, result.Name);
        Assert.Equal(account.CPF, result.CPF);
        Assert.Equal(account.Status, result.Status);

        var json = Encoding.UTF8.GetString(distributedCache.Get("account:42")!);
        Assert.Contains("\"cpf\":\"52998224725\"", json);
        Assert.Contains("\"status\":\"active\"", json);
    }

    [Fact]
    public async Task GetAsync_MissingKey_ReturnsNull()
    {
        var cache = new RedisCaching(new InMemoryDistributedCache());

        var result = await cache.GetAsync<Account>("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_RemovesCachedValue()
    {
        var cache = new RedisCaching(new InMemoryDistributedCache());
        await cache.SetAsync("account:42", "cached");

        await cache.RemoveAsync("account:42");

        Assert.Null(await cache.GetAsync<string>("account:42"));
    }

    [Fact]
    public async Task SetAsync_PassesExpirationToDistributedCache()
    {
        var distributedCache = new InMemoryDistributedCache();
        var cache = new RedisCaching(distributedCache);
        var expiration = TimeSpan.FromMinutes(10);

        await cache.SetAsync("key", "value", expiration);

        Assert.Equal(expiration, distributedCache.LastSetOptions!.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task SetAsync_NonPositiveExpiration_Throws()
    {
        var cache = new RedisCaching(new InMemoryDistributedCache());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => cache.SetAsync("key", "value", TimeSpan.Zero));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetAsync_EmptyKey_Throws(string key)
    {
        var cache = new RedisCaching(new InMemoryDistributedCache());

        await Assert.ThrowsAsync<ArgumentException>(() => cache.GetAsync<string>(key));
    }

    private sealed class InMemoryDistributedCache : IDistributedCache
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
}