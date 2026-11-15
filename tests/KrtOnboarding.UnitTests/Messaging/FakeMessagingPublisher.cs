using KrtOnboarding.Application.Messaging;

namespace KrtOnboarding.UnitTests.Messaging;

public class FakeMessagingPublisher : IMessagePublisher
{
    public Task PublishAsync<T>(T message, string exchangeName) where T : class
    {
        return Task.CompletedTask;
    }
}