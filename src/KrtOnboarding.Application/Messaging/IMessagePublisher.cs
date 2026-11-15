namespace KrtOnboarding.Application.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string exchangeName) where T : class;
}