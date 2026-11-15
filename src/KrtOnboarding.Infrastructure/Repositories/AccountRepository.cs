using KrtOnboarding.Domain.Entities;
using KrtOnboarding.Domain.Enums;
using KrtOnboarding.Domain.Repositories;
using KrtOnboarding.Domain.ValueObjects;
using KrtOnboarding.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KrtOnboarding.Infrastructure.Repositories;

public class AccountRepository(AppDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> GetByCpf(string cpf)
    {
        var query = dbContext.Accounts.AsNoTracking();

        if (!string.IsNullOrEmpty(cpf))
        {
            if (!Cpf.TryCreate(cpf, out var cpfValue))
                return null;

            query = query.Where(a => a.CPF == cpfValue);
        }

        return await query.FirstOrDefaultAsync();
    }

    public Task<Account?> GetByIdAsync(int id)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == id);
    }

    public async Task<Account> AddAsync(Account account)
    {
        await dbContext.Accounts.AddAsync(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(Account account)
    {
        dbContext.Accounts.Update(account);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Account account)
    {
        dbContext.Accounts.Remove(account);
        await dbContext.SaveChangesAsync();
    }
}
