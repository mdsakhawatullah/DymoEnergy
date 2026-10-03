using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace DymoEnergy.Shipping;

/// <summary>Reads and writes courier keys. Values are encrypted at rest with ABP's string encryption.</summary>
public class CourierVault : ITransientDependency
{
    private readonly IRepository<CourierCredential, int> _credentials;
    private readonly IStringEncryptionService _encryption;

    public CourierVault(IRepository<CourierCredential, int> credentials, IStringEncryptionService encryption)
    {
        _credentials = credentials;
        _encryption = encryption;
    }

    public async Task<string?> GetAsync(int accountId, CourierEnvironment env, string key)
    {
        var row = await _credentials.FirstOrDefaultAsync(c => c.CourierAccountId == accountId && c.Environment == env && c.Key == key);
        return row == null ? null : _encryption.Decrypt(row.EncryptedValue);
    }

    /// <summary>All stored keys of one courier, decrypted, with when each was last changed.</summary>
    public async Task<List<(CourierEnvironment Env, string Key, string Value, DateTime ChangedAt)>> GetAllAsync(int accountId)
    {
        var rows = await _credentials.GetListAsync(c => c.CourierAccountId == accountId);
        return rows.Select(r => (r.Environment, r.Key, _encryption.Decrypt(r.EncryptedValue) ?? "", r.LastModificationTime ?? r.CreationTime)).ToList();
    }

    /// <summary>Saves a key; an empty value removes it.</summary>
    public async Task SetAsync(int accountId, CourierEnvironment env, string key, string? value)
    {
        var row = await _credentials.FirstOrDefaultAsync(c => c.CourierAccountId == accountId && c.Environment == env && c.Key == key);
        if (string.IsNullOrEmpty(value))
        {
            if (row != null) await _credentials.DeleteAsync(row, autoSave: true);
            return;
        }

        var encrypted = _encryption.Encrypt(value)!;
        if (row == null)
            await _credentials.InsertAsync(new CourierCredential { CourierAccountId = accountId, Environment = env, Key = key, EncryptedValue = encrypted }, autoSave: true);
        else
        {
            row.EncryptedValue = encrypted;
            await _credentials.UpdateAsync(row, autoSave: true);
        }
    }

    public async Task ClearAsync(int accountId)
    {
        await _credentials.DeleteAsync(c => c.CourierAccountId == accountId, autoSave: true);
    }
}

/// <summary>Writes the courier call log in its own transaction, so failed calls are still recorded.</summary>
public class CourierLogWriter : ITransientDependency
{
    private readonly IRepository<CourierApiLog, int> _logs;
    private readonly IUnitOfWorkManager _uow;
    private readonly IClock _clock;
    private readonly ICurrentUser _user;

    public CourierLogWriter(IRepository<CourierApiLog, int> logs, IUnitOfWorkManager uow, IClock clock, ICurrentUser user)
    {
        _logs = logs; _uow = uow; _clock = clock; _user = user;
    }

    public async Task WriteAsync(int accountId, CourierEnvironment env, string action, string? method = null, string? endpoint = null,
        int? statusCode = null, int? durationMs = null, string? result = null, bool isError = false)
    {
        using var uow = _uow.Begin(requiresNew: true);
        await _logs.InsertAsync(new CourierApiLog
        {
            CourierAccountId = accountId, Environment = env, Time = _clock.Now, Action = action, Method = method,
            Endpoint = endpoint, StatusCode = statusCode, DurationMs = durationMs, IsError = isError, UserId = _user.Id,
            Result = result == null ? null : (result.Length > 250 ? result[..250] : result),
        });
        await uow.CompleteAsync();
    }

    public Task WriteAsync<T>(int accountId, CourierEnvironment env, string action, PathaoCall<T> call, string? okResult) =>
        WriteAsync(accountId, env, action, call.Method, call.Endpoint, call.StatusCode, call.DurationMs,
            call.Ok ? okResult : call.Error, !call.Ok);
}
