using KrtOnboarding.UnitTests.Messaging;
using KrtOnboarding.UnitTests.Caching;
using KrtOnboarding.Api.Controllers;
using KrtOnboarding.Api.Models;
using KrtOnboarding.Domain.Repositories;
using KrtOnboarding.Domain.Entities;
using KrtOnboarding.Domain.ValueObjects;
using KrtOnboarding.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using KrtOnboarding.Domain.Enums;
using KrtOnboarding.UnitTests.Logger;
using Microsoft.AspNetCore.Mvc;

namespace KrtOnboarding.UnitTests;

public class AccountControllerTest
{

    [Fact]
    public async Task Test_GetByIdAsync_ReturnsCorrectAccount()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(account);
        var result = await controller.Get(account.Id);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AccountResponse>(okResult.Value);

        Assert.Equal(account.Id, response.Id);
        Assert.Equal(account.Name, response.Name);
        Assert.Equal(account.CPF.Value, response.CPF);
        Assert.Equal(account.Status, response.Status);
    }

    [Fact]
    public async Task Test_GetByIdAsync_ReturnsNotFoundForNonExistentAccount()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var result = await controller.Get(999);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Test_GetByIdAsync_CachesAccountAfterRetrieval()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(account);
        var result = await controller.Get(account.Id);
        var cached = await redisCaching.GetAsync<Account>($"account:{account.Id}");
        
        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(account.Id, cached?.Id);
    }

    [Fact]
    public async Task Test_GetByIdAsync_ReturnsCachedAccount()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await redisCaching.SetAsync($"account:{account.Id}", account);
        await controller.Get(account.Id);
        var result = await controller.Get(account.Id);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AccountResponse>(okResult.Value);

        Assert.Equal(account.Id, response.Id);
    }

    [Fact]
    public async Task Test_GetByIdAsync_ReturnsCachedAccountOnSubsequentCalls()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(account);
        await controller.Get(account.Id);
        var result = await controller.Get(account.Id);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AccountResponse>(okResult.Value);

        Assert.Equal(account.Id, response.Id);
    }

    [Fact]
    public async Task Test_CreateAccount_ReturnsCreatedResult()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var createAccount = new CreateAccount
        {
            Name = "Jane Doe",
            CPF = "52998224725",
            Status = AccountStatus.Active
        };
        var result = await controller.CreateAccount(createAccount);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AccountResponse>(createdResult.Value);

        Assert.Equal(createAccount.Name, response.Name);
        Assert.Equal(createAccount.CPF, response.CPF);
        Assert.Equal(createAccount.Status, response.Status);
    }

    [Fact]
    public async Task Test_CreateAccount_ReturnsBadRequestForInvalidCPF()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var createAccount = new CreateAccount
        {
            Name = "Jane Doe",
            CPF = "11111111111",
            Status = AccountStatus.Active
        };
        var result = await controller.CreateAccount(createAccount);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Test_CreateAccount_ReturnsConflictForExistingCPF()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var createAccount = new CreateAccount
        {
            Name = "Jane Doe",
            CPF = "91117261484",
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(new Account
        {
            Name = "Existing User",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        });
        var result = await controller.CreateAccount(createAccount);
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Test_CreateAccount_CachesAccountAfterCreation()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var createAccount = new CreateAccount
        {
            Name = "Jane Doe",
            CPF = "52998224725",
            Status = AccountStatus.Active
        };
        var result = await controller.CreateAccount(createAccount);
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<AccountResponse>(createdResult.Value);

        var cached = await redisCaching.GetAsync<Account>($"account:{response.Id}");
        Assert.NotNull(cached);
        Assert.Equal(response.Id, cached?.Id);
    }

    [Fact]
    public async Task Test_UpdateAccount_ReturnsUpdatedAccount()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(account);

        var updateAccount = new UpdateAccount
        {
            Name = "John Smith",
            Status = AccountStatus.Inactive
        };

        var result = await controller.UpdateAccount(account.Id, updateAccount);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AccountResponse>(okResult.Value);

        Assert.Equal(updateAccount.Name, response.Name);
        Assert.Equal(updateAccount.Status, response.Status);
    }

    [Fact]
    public async Task Test_UpdateAccount_ReturnsNotFoundForNonExistentAccount()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var updateAccount = new UpdateAccount
        {
            Name = "John Smith",
            Status = AccountStatus.Inactive
        };
        var result = await controller.UpdateAccount(999, updateAccount);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Test_UpdateAccount_CachesUpdatedAccount()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(account);

        var updateAccount = new UpdateAccount
        {
            Name = "John Smith",
            Status = AccountStatus.Inactive
        };

        await controller.UpdateAccount(account.Id, updateAccount);
        var cached = await redisCaching.GetAsync<Account>($"account:{account.Id}");

        Assert.NotNull(cached);
        Assert.Equal(updateAccount.Name, cached?.Name);
        Assert.Equal(updateAccount.Status, cached?.Status);
    }

    [Fact]
    public async Task Test_DeleteAccount_ReturnsNoContent()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(account);

        var result = await controller.DeleteAccount(account.Id);
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Test_DeleteAccount_RemovesAccountFromRepositoryAndCache()
    {
        var fakeRepository = new FakeRepository();
        var redisCaching = GetRedisCaching();
        var controller = CreateAccountController(fakeRepository, redisCaching);
        var account = new Account
        {
            Name = "John Doe",
            CPF = Cpf.Create("91117261484"),
            Status = AccountStatus.Active
        };
        await fakeRepository.AddAsync(account);
        await redisCaching.SetAsync($"account:{account.Id}", account);

        var result = await controller.DeleteAccount(account.Id);
        Assert.IsType<NoContentResult>(result);

        var deletedAccount = await fakeRepository.GetByIdAsync(account.Id);
        Assert.Null(deletedAccount);

        var cachedAccount = await redisCaching.GetAsync<Account>($"account:{account.Id}");
        Assert.Null(cachedAccount);
    }

    private sealed class FakeRepository: IAccountRepository
    {
        private Dictionary<int, Account> _accounts = new Dictionary<int, Account>();
        public Task<Account?> GetByIdAsync(int id)
        {
            return Task.FromResult<Account?>(_accounts.GetValueOrDefault(id));
        }

        public Task<Account?> GetByCpf(string cpf)
        {
            var found = _accounts.Values.FirstOrDefault(a => a.CPF.Value == cpf);
            return Task.FromResult<Account?>(found);
        }

        public Task<Account> AddAsync(Account account)
        {
            _accounts[account.Id] = account;
            return Task.FromResult<Account>(account);
        }

        public Task UpdateAsync(Account account)
        {
            _accounts[account.Id] = account;
            return Task.CompletedTask;
        }
        public Task DeleteAsync(Account account)
        {
            _accounts.Remove(account.Id);
            return Task.CompletedTask;
        }
    }

    private RedisCaching GetRedisCaching()
    {
        return new RedisCaching(new InMemoryDistributedCache(), new FakeLogger<RedisCaching>());
    }

    private AccountController CreateAccountController(FakeRepository fakeRepository, RedisCaching redisCaching)
    {
        var distributedCache = new InMemoryDistributedCache();
        return new AccountController(
            accountRepository: fakeRepository,
            redisCaching: redisCaching,
            messagePublisher: new FakeMessagingPublisher(),
            logger: new FakeLogger<AccountController>(),
            configuration: new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CACHING_EXPIRATION_HOURS", "24" }
            })
                .Build()
        );
    }

    
}