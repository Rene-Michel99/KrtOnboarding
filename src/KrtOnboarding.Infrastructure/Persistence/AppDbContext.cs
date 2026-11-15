using KrtOnboarding.Domain.Entities;
using KrtOnboarding.Domain.Enums;
using KrtOnboarding.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace KrtOnboarding.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>()
            .Property(account => account.CPF)
            .HasConversion(cpf => cpf.Value, value => Cpf.Create(value))
            .HasMaxLength(11);

        modelBuilder.Entity<Account>()
            .Property(account => account.Status)
            .HasConversion(
                status => ToStorage(status),
                value => FromStorage(value));
    }

    private static string ToStorage(AccountStatus status) => status switch
    {
        AccountStatus.Active => "active",
        AccountStatus.Inactive => "inactive",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static AccountStatus FromStorage(string value) => value.ToLowerInvariant() switch
    {
        "active" => AccountStatus.Active,
        "inactive" => AccountStatus.Inactive,
        _ => throw new InvalidOperationException($"Unknown account status: {value}")
    };
}
