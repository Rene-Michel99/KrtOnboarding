using KrtOnboarding.Domain.Entities;
using KrtOnboarding.Domain.Enums;

namespace KrtOnboarding.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByCpf(string cpf);
    Task<Account?> GetByIdAsync(int id);
    Task<Account> AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(Account account);
}