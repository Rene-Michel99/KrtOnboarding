using KrtOnboarding.UnitTests.Caching;
using System.Text;
using KrtOnboarding.Domain.Entities;
using KrtOnboarding.Domain.Enums;
using KrtOnboarding.Domain.ValueObjects;
using KrtOnboarding.Infrastructure.Caching;
using KrtOnboarding.UnitTests.Logger;

namespace KrtOnboarding.UnitTests;

public class CacheTests
{
    [Fact]
    public async Task SetAsync_Account_CanBeRetrievedWithCpfAndStatus()
    {
        var distributedCache = new InMemoryDistributedCache();
        var cache = new RedisCaching(distributedCache, new FakeLogger<RedisCaching>());
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
        var cache = new RedisCaching(new InMemoryDistributedCache(), new FakeLogger<RedisCaching>());

        var result = await cache.GetAsync<Account>("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_RemovesCachedValue()
    {
        var cache = new RedisCaching(new InMemoryDistributedCache(), new FakeLogger<RedisCaching>());
        await cache.SetAsync("account:42", "cached");

        await cache.RemoveAsync("account:42");

        Assert.Null(await cache.GetAsync<string>("account:42"));
    }

    [Fact]
    public async Task SetAsync_PassesExpirationToDistributedCache()
    {
        var distributedCache = new InMemoryDistributedCache();
        var cache = new RedisCaching(distributedCache, new FakeLogger<RedisCaching>());
        var expiration = TimeSpan.FromMinutes(10);

        await cache.SetAsync("key", "value", expiration);

        Assert.Equal(expiration, distributedCache.LastSetOptions!.AbsoluteExpirationRelativeToNow);
    }
}