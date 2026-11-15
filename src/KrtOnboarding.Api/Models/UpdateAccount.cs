using KrtOnboarding.Domain.Enums;

namespace KrtOnboarding.Api.Models;

public class UpdateAccount
{
    public string? Name { get; set; }
    public string? CPF { get; set; }
    public required AccountStatus Status { get; set; }
}