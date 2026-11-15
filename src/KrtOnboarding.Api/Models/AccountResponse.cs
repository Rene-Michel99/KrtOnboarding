using KrtOnboarding.Domain.Enums;

namespace KrtOnboarding.Api.Models;

public class AccountResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string CPF { get; set; }
    public required AccountStatus Status { get; set; }
}