using System;
using System.Threading.Tasks;
using Xunit;
using KafkaConsumerService.Infrastructure;

namespace KafkaConsumerService.Tests;

public class IdempotencyStoreTests
{
    private readonly IIdempotencyStore _idempotencyStore;

    public IdempotencyStoreTests()
    {
        _idempotencyStore = new IdempotencyStore();
    }

    [Fact]
    public async Task StoreAsync_ShouldStoreKeySuccessfully()
    {
        var key = Guid.NewGuid().ToString();
        await _idempotencyStore.StoreAsync(key);
        var exists = await _idempotencyStore.ExistsAsync(key);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueForStoredKey()
    {
        var key = Guid.NewGuid().ToString();
        await _idempotencyStore.StoreAsync(key);

        var exists = await _idempotencyStore.ExistsAsync(key);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseForNonStoredKey()
    {
        var key = Guid.NewGuid().ToString();

        var exists = await _idempotencyStore.ExistsAsync(key);

        Assert.False(exists);
    }
}