using KrtOnboarding.Api.Models;
using KrtOnboarding.Application.Messaging;
using KrtOnboarding.Application.Messaging.Events;
using KrtOnboarding.Application.Caching;
using KrtOnboarding.Domain.Entities;
using KrtOnboarding.Domain.Enums;
using KrtOnboarding.Domain.Repositories;
using KrtOnboarding.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KrtOnboarding.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<AccountController> _logger;

    private readonly IRedisCaching _redisCaching;
    private readonly TimeSpan _cacheExpiration;

    public AccountController(
        IAccountRepository accountRepository,
        IMessagePublisher messagePublisher,
        ILogger<AccountController> logger,
        IRedisCaching redisCaching,
        IConfiguration configuration)
    {
        _accountRepository = accountRepository;
        _messagePublisher = messagePublisher;
        _logger = logger;
        _redisCaching = redisCaching;

        var expirationHours = configuration.GetValue<int?>("CACHING_EXPIRATION_HOURS", 24);
        if (!expirationHours.HasValue || expirationHours.Value <= 0)
            throw new InvalidOperationException("CACHING_EXPIRATION_HOURS must be a positive integer.");

        _cacheExpiration = TimeSpan.FromHours(expirationHours.Value);
    }

    [HttpGet(Name = "GetAccounts")]
    public async Task<ActionResult<AccountResponse>> Get(int id)
    {
        var cachedAccount = await _redisCaching.GetAsync<Account>(AccountCacheKey(id));
        if (cachedAccount is not null)
            return Ok(ToResponse(cachedAccount));
        
        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null)
            return NotFound();

        await _redisCaching.SetAsync(AccountCacheKey(account.Id), account, _cacheExpiration);
        return Ok(ToResponse(account));
    }

    [HttpPost(Name = "CreateAccount")]
    public async Task<ActionResult<AccountResponse>> CreateAccount([FromBody] CreateAccount createAccount)
    {
        if (!Cpf.TryCreate(createAccount.CPF, out var cpf))
            return BadRequest(new { message = "CPF inválido." });
        
        var existingAccount = await _accountRepository.GetByCpf(createAccount.CPF);
        if (existingAccount != null)
            return Conflict(new { message = "Já existe uma conta com este CPF." });
        
        var account = new Account
        {
            Name = createAccount.Name,
            CPF = cpf,
            Status = createAccount.Status
        };

        try
        {    
            var addedAccount = await _accountRepository.AddAsync(account);
            await _redisCaching.SetAsync(AccountCacheKey(addedAccount.Id), addedAccount, _cacheExpiration);
            await _messagePublisher.PublishAsync(new AccountCreatedEvent(
                addedAccount.Id,
                addedAccount.Name,
                addedAccount.Status.ToString().ToLowerInvariant()), "accountCreated");

            var response = ToResponse(addedAccount);

            return CreatedAtAction(nameof(Get), new { id = addedAccount.Id }, response);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar a conta.");
            return StatusCode(500, new { message = "Ocorreu um erro ao criar a conta." });
        }
    }

    [HttpPut("{id}", Name = "UpdateAccount")]
    public async Task<ActionResult<AccountResponse>> UpdateAccount(int id, [FromBody] UpdateAccount updateAccount)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null)
            return NotFound();

        if (updateAccount.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(updateAccount.Name))
                return BadRequest(new { message = "Nome inválido." });

            account.Name = updateAccount.Name;
        }

        if (updateAccount.CPF is not null)
        {
            if (!Cpf.TryCreate(updateAccount.CPF!, out var cpf))
                return BadRequest(new { message = "CPF inválido." });

            account.CPF = cpf;
        }

        account.Status = updateAccount.Status;

        try
        {    
            await _accountRepository.UpdateAsync(account);
            await _redisCaching.SetAsync(AccountCacheKey(account.Id), account, _cacheExpiration);
            var response = ToResponse(account);
            await _messagePublisher.PublishAsync(new AccountUpdatedEvent(
                account.Id,
                account.Name,
                account.Status.ToString().ToLowerInvariant()), "accountUpdated");

            return Ok(response);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar a conta.");
            return StatusCode(500, new { message = "Ocorreu um erro ao atualizar a conta." });
        }
    }

    [HttpDelete("{id}", Name = "DeleteAccount")]
    public async Task<ActionResult> DeleteAccount(int id)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null)
            return NotFound();

        try
        {    
            await _accountRepository.DeleteAsync(account);
            await _redisCaching.RemoveAsync(AccountCacheKey(account.Id));
            await _messagePublisher.PublishAsync(new AccountDeletedEvent(
                account.Id,
                account.Name,
                account.Status.ToString().ToLowerInvariant()), "accountDeleted");

            return NoContent();
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar a conta.");
            return StatusCode(500, new { message = "Ocorreu um erro ao deletar a conta." });
        }
    }

    private static AccountResponse ToResponse(Account account)
    {
        return new AccountResponse
        {
            Id = account.Id,
            Name = account.Name,
            CPF = account.CPF.Value,
            Status = account.Status
        };
    }

    private static string AccountCacheKey(int id) => $"account:{id}";
}