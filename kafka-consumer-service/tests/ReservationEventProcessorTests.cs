using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using KafkaConsumerService.Services;
using KafkaConsumerService.Infrastructure;
using KafkaConsumerService.Models;
using KafkaConsumerService.Logging;

namespace KafkaConsumerService.Tests;

[TestFixture]
public class ReservationEventProcessorTests
{
    private Mock<IIdempotencyStore> _idempotencyStoreMock;
    private Mock<IRetryPolicy> _retryPolicyMock;
    private Mock<ConsumerLogger> _loggerMock;
    private ReservationEventProcessor _processor;

    [SetUp]
    public void Setup()
    {
        _idempotencyStoreMock = new Mock<IIdempotencyStore>();
        _retryPolicyMock = new Mock<IRetryPolicy>();
        _loggerMock = new Mock<ConsumerLogger>(null);
        _processor = new ReservationEventProcessor(_idempotencyStoreMock.Object, _retryPolicyMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task ProcessAsync_ShouldStoreIdempotencyKey_WhenProcessingSucceeds()
    {
        var json = "{\"id\":\"1\",\"reservationId\":\"R1\",\"customerId\":\"C1\",\"reservationDate\":\"2026-05-07T00:00:00\",\"status\":\"Created\"}";
        _idempotencyStoreMock.Setup(x => x.ExistsAsync("1")).ReturnsAsync(false);
        _retryPolicyMock.Setup(x => x.ExecuteAsync(It.IsAny<Func<Task>>())).Returns(async (Func<Task> action) => await action());

        await _processor.ProcessAsync(json);

        _idempotencyStoreMock.Verify(x => x.StoreAsync("1"), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_ShouldNotProcessDuplicateEvents()
    {
        var json = "{\"id\":\"1\",\"reservationId\":\"R1\",\"customerId\":\"C1\",\"reservationDate\":\"2026-05-07T00:00:00\",\"status\":\"Created\"}";
        _idempotencyStoreMock.Setup(x => x.ExistsAsync("1")).ReturnsAsync(true);

        await _processor.ProcessAsync(json);

        _idempotencyStoreMock.Verify(x => x.StoreAsync(It.IsAny<string>()), Times.Never);
    }
}