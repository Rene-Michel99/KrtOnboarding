namespace KrtOnboarding.Application.Messaging.Events;

public sealed record AccountDeletedEvent(
    int AccountId,
    string Name,
    string Status);