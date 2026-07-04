using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using DymoEnergy.Shared;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.Companies;

[Authorize(DymoEnergyPermissions.Companies.Default)]
public class CompanyAppService : ApplicationService, ICompanyAppService
{
    private readonly IRepository<Company, int> _companyRepository;

    public CompanyAppService(IRepository<Company, int> companyRepository)
    {
        _companyRepository = companyRepository;
    }

    // ── READ ──────────────────────────────────────────────────────────────

    public async Task<CompanyDto> GetAsync(int id)
    {
        var company = await _companyRepository.GetAsync(id);
        return MapToDto(company);
    }

    public async Task<DymoPagedResultDto<CompanyDto>> GetListDataAsync(CompanyFilterDto input)
    {
        var query = await _companyRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(c =>
                (c.CompanyName != null && c.CompanyName.Contains(input.Filter)) ||
                (c.CompanyCode != null && c.CompanyCode.Contains(input.Filter)) ||
                (c.Email       != null && c.Email.Contains(input.Filter))       ||
                (c.Phone       != null && c.Phone.Contains(input.Filter)));

        if (input.Status.HasValue)          query = query.Where(c => c.Status          == input.Status);
        if (input.IsParentCompany.HasValue) query = query.Where(c => c.IsParentCompany == input.IsParentCompany);
        if (input.ParentCompanyId.HasValue) query = query.Where(c => c.ParentCompanyId == input.ParentCompanyId);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query
            .OrderBy(c => c.CompanyName)
            .ThenByDescending(c => c.Id)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var companies = await AsyncExecuter.ToListAsync(query);
        return new DymoPagedResultDto<CompanyDto>(totalCount, companies.Select(MapToDto).ToList());
    }

    public async Task<List<SelectListDto>> GetSelectListAsync()
    {
        var query = (await _companyRepository.GetQueryableAsync())
            .OrderBy(c => c.CompanyName)
            .Select(c => new SelectListDto
            {
                Value       = c.Id,
                DisplayText = c.CompanyName,
            });

        return await AsyncExecuter.ToListAsync(query);
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.Companies.Create)]
    public async Task<CompanyDto> CreateCompanyDataAsync(CreateUpdateCompanyDto input)
    {
        var company = new Company();
        ApplyInput(company, input);

        await _companyRepository.InsertAsync(company, autoSave: true);
        return MapToDto(company);
    }

    [Authorize(DymoEnergyPermissions.Companies.Edit)]
    public async Task<CompanyDto> UpdateAsync(int id, CreateUpdateCompanyDto input)
    {
        var company = await _companyRepository.GetAsync(id);
        ApplyInput(company, input);
        await _companyRepository.UpdateAsync(company, autoSave: true);
        return MapToDto(company);
    }

    [Authorize(DymoEnergyPermissions.Companies.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _companyRepository.DeleteAsync(id, autoSave: true);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private static void ApplyInput(Company c, CreateUpdateCompanyDto input)
    {
        c.CompanyName         = input.CompanyName;
        c.CompanyCode         = input.CompanyCode;
        c.Email                = input.Email;
        c.Phone                = input.Phone;
        c.Website              = input.Website;
        c.TaxId                = input.TaxId;
        c.Address              = input.Address;
        c.City                 = input.City;
        c.State                = input.State;
        c.PostalCode           = input.PostalCode;
        c.Country              = input.Country;
        c.ContactPersonName    = input.ContactPersonName;
        c.ContactPersonEmail   = input.ContactPersonEmail;
        c.ContactPersonPhone   = input.ContactPersonPhone;
        c.Notes                = input.Notes;
        c.Status                = input.Status;
        c.SendStatementTo      = input.SendStatementTo;
        c.IsParentCompany      = input.IsParentCompany;
        c.HasChildCompany      = input.HasChildCompany;
        c.ParentCompanyId      = input.ParentCompanyId;
        c.DefaultShippingFee    = input.DefaultShippingFee;
        c.FreeShippingThreshold = input.FreeShippingThreshold;
        c.IsFlatRateShipping    = input.IsFlatRateShipping;
        c.ShippingCurrencyCode  = input.ShippingCurrencyCode ?? "BDT";
        c.ExternalAccountId    = input.ExternalAccountId;
        c.ApiKey                = input.ApiKey;
        c.WebhookUrl            = input.WebhookUrl;
        c.IsSyncEnabled        = input.IsSyncEnabled;
        c.LastSyncedAt          = input.LastSyncedAt;
    }

    private static CompanyDto MapToDto(Company c) => new()
    {
        Id                    = c.Id,
        CreationTime          = c.CreationTime,
        CreatorId             = c.CreatorId,
        LastModificationTime  = c.LastModificationTime,
        LastModifierId        = c.LastModifierId,
        IsDeleted             = c.IsDeleted,
        DeletionTime          = c.DeletionTime,
        DeleterId             = c.DeleterId,
        CompanyName           = c.CompanyName,
        CompanyCode           = c.CompanyCode,
        Email                 = c.Email,
        Phone                 = c.Phone,
        Website               = c.Website,
        TaxId                 = c.TaxId,
        Address               = c.Address,
        City                  = c.City,
        State                 = c.State,
        PostalCode            = c.PostalCode,
        Country               = c.Country,
        ContactPersonName     = c.ContactPersonName,
        ContactPersonEmail    = c.ContactPersonEmail,
        ContactPersonPhone    = c.ContactPersonPhone,
        Notes                 = c.Notes,
        Status                = c.Status,
        SendStatementTo       = c.SendStatementTo,
        IsParentCompany       = c.IsParentCompany,
        HasChildCompany       = c.HasChildCompany,
        ParentCompanyId       = c.ParentCompanyId,
        DefaultShippingFee    = c.DefaultShippingFee,
        FreeShippingThreshold = c.FreeShippingThreshold,
        IsFlatRateShipping    = c.IsFlatRateShipping,
        ShippingCurrencyCode  = c.ShippingCurrencyCode,
        ExternalAccountId     = c.ExternalAccountId,
        ApiKey                = c.ApiKey,
        WebhookUrl            = c.WebhookUrl,
        IsSyncEnabled         = c.IsSyncEnabled,
        LastSyncedAt          = c.LastSyncedAt,
    };
}
