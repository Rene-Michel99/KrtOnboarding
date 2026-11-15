namespace KrtOnboarding.Application.Messaging.Events;

public sealed record AccountUpdatedEvent(
    int AccountId,
    string Name,
    string Status);