using KrtOnboarding.Domain.Enums;
using KrtOnboarding.Domain.ValueObjects;

namespace KrtOnboarding.Domain.Entities;

public class Account
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required Cpf CPF { get; set; }
    public required AccountStatus Status { get; set; }
}