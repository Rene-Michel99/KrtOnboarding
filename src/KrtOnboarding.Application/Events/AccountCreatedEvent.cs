namespace KrtOnboarding.Application.Messaging.Events;

public sealed record AccountCreatedEvent(
    int AccountId,
    string Name,
    string Status);