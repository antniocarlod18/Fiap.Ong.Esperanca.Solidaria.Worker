using Fiap.Ong.Esperanca.Solidaria.Contracts.Events;
using Fiap.Ong.Esperanca.Solidaria.Worker.Api.Consumers;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Entities;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Tests.Consumers;

public class DonationReceivedConsumerTests
{
    [Fact]
    public async Task Consume_WhenDonationReceived_ShouldPublishDonationProcessedEventWithSameData()
    {
        var donationRepositoryMock = new Mock<IDonationRepository>();
        donationRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Donation>()))
            .Returns(Task.CompletedTask);

        var consumer = new DonationReceivedConsumer(new NullLogger<DonationReceivedConsumer>(), donationRepositoryMock.Object);

        var receivedEvent = new DonationReceivedEvent
        {
            CampaignId = "cmp-001",
            CampaignTitle = "Campanha Teste",
            DonorId = "donor-123",
            Amount = 250.75m,
            Timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var contextMock = new Mock<ConsumeContext<DonationReceivedEvent>>();
        contextMock.SetupGet(x => x.Message).Returns(receivedEvent);

        DonationProcessedEvent? publishedEvent = null;

        contextMock
            .Setup(x => x.Publish(It.IsAny<DonationProcessedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((message, _) => publishedEvent = (DonationProcessedEvent)message)
            .Returns(Task.CompletedTask);

        await consumer.Consume(contextMock.Object);

        Assert.NotNull(publishedEvent);
        Assert.Equal(receivedEvent.CampaignId, publishedEvent!.CampaignId);
        Assert.Equal(receivedEvent.DonorId, publishedEvent.DonorId);
        Assert.Equal(receivedEvent.Amount, publishedEvent.Amount);
        Assert.Equal(receivedEvent.Timestamp, publishedEvent.Timestamp);

        donationRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Donation>()), Times.Once);
    }

    [Fact]
    public async Task Consume_WhenPublishFails_ShouldPropagateException()
    {
        var donationRepositoryMock = new Mock<IDonationRepository>();
        donationRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Donation>()))
            .Returns(Task.CompletedTask);

        var consumer = new DonationReceivedConsumer(new NullLogger<DonationReceivedConsumer>(), donationRepositoryMock.Object);

        var receivedEvent = new DonationReceivedEvent
        {
            CampaignId = "cmp-001",
            CampaignTitle = "Campanha Teste",
            DonorId = "donor-123",
            Amount = 250.75m,
            Timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var contextMock = new Mock<ConsumeContext<DonationReceivedEvent>>();
        contextMock.SetupGet(x => x.Message).Returns(receivedEvent);

        var expectedException = new InvalidOperationException("publish error");

        contextMock
            .Setup(x => x.Publish(It.IsAny<DonationProcessedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(contextMock.Object));

        Assert.Equal("publish error", ex.Message);
        donationRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Donation>()), Times.Once);
    }
}
