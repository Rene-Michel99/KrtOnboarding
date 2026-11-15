using System.Text.Json;
using System.Text.Json.Serialization;
using KrtOnboarding.Application.Caching;
using KrtOnboarding.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Distributed;

namespace KrtOnboarding.Infrastructure.Caching;

public sealed class RedisCaching(IDistributedCache cache) : IRedisCaching
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        ValidateKey(key);

        if (expiration.HasValue && expiration.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(expiration), "Expiration must be greater than zero.");

        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiration.Value;

        var serializedValue = JsonSerializer.Serialize(value, JsonOptions);
        await cache.SetStringAsync(key, serializedValue, options);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        ValidateKey(key);

        var serializedValue = await cache.GetStringAsync(key);
        return serializedValue is null ? default : JsonSerializer.Deserialize<T>(serializedValue, JsonOptions);
    }

    public Task RemoveAsync(string key)
    {
        ValidateKey(key);
        return cache.RemoveAsync(key);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new CpfJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        return options;
    }

    private sealed class CpfJsonConverter : JsonConverter<Cpf>
    {
        public override Cpf Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String ||
                !Cpf.TryCreate(reader.GetString(), out var cpf))
            {
                throw new JsonException("Cached CPF is invalid.");
            }

            return cpf;
        }

        public override void Write(Utf8JsonWriter writer, Cpf value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}