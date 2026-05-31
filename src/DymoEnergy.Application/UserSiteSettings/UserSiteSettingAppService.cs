using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.UserSiteSettings;

[Authorize(DymoEnergyPermissions.UserSiteSettings.Default)]
public class UserSiteSettingAppService : ApplicationService, IUserSiteSettingAppService
{
    private readonly IRepository<UserSiteSetting, int>      _repository;
    private readonly IRepository<UserSiteSettingImage, int> _imageRepository;

    public UserSiteSettingAppService(
        IRepository<UserSiteSetting, int>      repository,
        IRepository<UserSiteSettingImage, int> imageRepository)
    {
        _repository      = repository;
        _imageRepository = imageRepository;
    }

    // ── READ ──────────────────────────────────────────────────────────────

    public async Task<UserSiteSettingDto> GetAsync(int id)
    {
        var entity = await _repository.GetAsync(id);
        var images = await LoadImagesAsync(id);
        return MapToDto(entity, images);
    }

    public async Task<PagedResultDto<UserSiteSettingDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var totalCount = await _repository.GetCountAsync();
        var items      = await _repository.GetPagedListAsync(
            input.SkipCount, input.MaxResultCount,
            input.Sorting ?? nameof(UserSiteSetting.Id));

        return new PagedResultDto<UserSiteSettingDto>(
            totalCount,
            items.Select(s => MapToDto(s, new List<UserSiteSettingImage>())).ToList());
    }

    public async Task<UserSiteSettingDto> GetActiveAsync()
    {
        var entity = await _repository.FindAsync(s => s.IsActive);
        if (entity == null) return null;
        var images = await LoadImagesAsync(entity.Id);
        return MapToDto(entity, images);
    }

    // ── WRITE ─────────────────────────────────────────────────────────────

    [Authorize(DymoEnergyPermissions.UserSiteSettings.Create)]
    public async Task<UserSiteSettingDto> CreateAsync(CreateUpdateUserSiteSettingDto input)
    {
        var entity = new UserSiteSetting();
        ApplyInput(entity, input);
        await _repository.InsertAsync(entity, autoSave: true);

        var images = new List<UserSiteSettingImage>();
        if (input.Images.Count > 0)
        {
            images = input.Images.Select(dto => MapToImage(dto, entity.Id)).ToList();
            await _imageRepository.InsertManyAsync(images, autoSave: true);
        }

        return MapToDto(entity, images);
    }

    [Authorize(DymoEnergyPermissions.UserSiteSettings.Edit)]
    public async Task<UserSiteSettingDto> UpdateAsync(int id, CreateUpdateUserSiteSettingDto input)
    {
        var entity = await _repository.GetAsync(id);
        ApplyInput(entity, input);
        await _repository.UpdateAsync(entity, autoSave: true);

        var imageQuery    = await _imageRepository.GetQueryableAsync();
        var currentImages = await AsyncExecuter.ToListAsync(
            imageQuery.Where(i => i.UserSiteSettingId == id));

        var inputWithId = input.Images.Where(i => i.Id is > 0).ToList();
        var inputNew    = input.Images.Where(i => !(i.Id is > 0)).ToList();
        var keepIds     = inputWithId.Select(i => i.Id!.Value).ToHashSet();

        // 1. Delete removed images
        var toDelete = currentImages.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (toDelete.Count > 0)
            await _imageRepository.DeleteManyAsync(toDelete, autoSave: true);

        // 2. Update existing images
        var toUpdate = new List<UserSiteSettingImage>();
        foreach (var dto in inputWithId)
        {
            var existing = currentImages.FirstOrDefault(i => i.Id == dto.Id);
            if (existing == null) continue;
            ApplyImageInput(existing, dto);
            toUpdate.Add(existing);
        }
        if (toUpdate.Count > 0)
            await _imageRepository.UpdateManyAsync(toUpdate, autoSave: true);

        // 3. Insert new images
        var toInsert = inputNew.Select(dto => MapToImage(dto, id)).ToList();
        if (toInsert.Count > 0)
            await _imageRepository.InsertManyAsync(toInsert, autoSave: true);

        var finalImages = toUpdate.Concat(toInsert).OrderBy(i => i.DisplayOrder).ToList();
        return MapToDto(entity, finalImages);
    }

    [Authorize(DymoEnergyPermissions.UserSiteSettings.Delete)]
    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.UserSiteSettings.Edit)]
    public async Task<UserSiteSettingDto> SetActiveAsync(int id)
    {
        var all = await _repository.GetListAsync();
        foreach (var s in all.Where(s => s.IsActive))
        {
            s.IsActive = false;
            await _repository.UpdateAsync(s);
        }

        var target = await _repository.GetAsync(id);
        target.IsActive = true;
        await _repository.UpdateAsync(target, autoSave: true);

        var images = await LoadImagesAsync(id);
        return MapToDto(target, images);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private async Task<List<UserSiteSettingImage>> LoadImagesAsync(int settingId)
    {
        var query = await _imageRepository.GetQueryableAsync();
        return await AsyncExecuter.ToListAsync(
            query.Where(i => i.UserSiteSettingId == settingId)
                 .OrderBy(i => i.DisplayOrder));
    }

    private static void ApplyInput(UserSiteSetting entity, CreateUpdateUserSiteSettingDto input)
    {
        entity.PortalId               = input.PortalId;
        entity.BackgroundImage        = input.BackgroundImage;
        entity.Description            = input.Description;
        entity.ButtonColor            = input.ButtonColor;
        entity.PrimaryColor           = input.PrimaryColor;
        entity.BodyColor              = input.BodyColor;
        entity.BackgroundColor        = input.BackgroundColor;
        entity.CardBgColor            = input.CardBgColor;
        entity.NavbarBgColor          = input.NavbarBgColor;
        entity.NavbarTextColor        = input.NavbarTextColor;
        entity.SidebarBgColor         = input.SidebarBgColor;
        entity.SidebarTextColor       = input.SidebarTextColor;
        entity.SidebarActiveBgColor   = input.SidebarActiveBgColor;
        entity.ButtonPrimaryBgColor   = input.ButtonPrimaryBgColor;
        entity.ButtonPrimaryTextColor = input.ButtonPrimaryTextColor;
        entity.FontFamily             = input.FontFamily;
        entity.FontSizeBase           = input.FontSizeBase;
        entity.IsActive               = input.IsActive;
    }

    private static UserSiteSettingImage MapToImage(CreateUpdateUserSiteSettingImageDto dto, int settingId) => new()
    {
        UserSiteSettingId = settingId,
        ImageUrl          = dto.ImageUrl,
        Title             = dto.Title,
        AltText           = dto.AltText,
        DisplayOrder      = dto.DisplayOrder,
        IsActive          = dto.IsActive
    };

    private static void ApplyImageInput(UserSiteSettingImage img, CreateUpdateUserSiteSettingImageDto dto)
    {
        img.ImageUrl     = dto.ImageUrl;
        img.Title        = dto.Title;
        img.AltText      = dto.AltText;
        img.DisplayOrder = dto.DisplayOrder;
        img.IsActive     = dto.IsActive;
    }

    private static UserSiteSettingDto MapToDto(UserSiteSetting s, List<UserSiteSettingImage> images) => new()
    {
        Id                    = s.Id,
        CreationTime          = s.CreationTime,
        CreatorId             = s.CreatorId,
        LastModificationTime  = s.LastModificationTime,
        LastModifierId        = s.LastModifierId,
        IsDeleted             = s.IsDeleted,
        DeletionTime          = s.DeletionTime,
        DeleterId             = s.DeleterId,
        PortalId              = s.PortalId,
        BackgroundImage       = s.BackgroundImage,
        Description           = s.Description,
        ButtonColor           = s.ButtonColor,
        PrimaryColor          = s.PrimaryColor,
        BodyColor             = s.BodyColor,
        BackgroundColor       = s.BackgroundColor,
        CardBgColor           = s.CardBgColor,
        NavbarBgColor         = s.NavbarBgColor,
        NavbarTextColor       = s.NavbarTextColor,
        SidebarBgColor        = s.SidebarBgColor,
        SidebarTextColor      = s.SidebarTextColor,
        SidebarActiveBgColor  = s.SidebarActiveBgColor,
        ButtonPrimaryBgColor  = s.ButtonPrimaryBgColor,
        ButtonPrimaryTextColor = s.ButtonPrimaryTextColor,
        FontFamily            = s.FontFamily,
        FontSizeBase          = s.FontSizeBase,
        IsActive              = s.IsActive,
        Images = images.Select(i => new UserSiteSettingImageDto
        {
            Id                = i.Id,
            UserSiteSettingId = i.UserSiteSettingId,
            ImageUrl          = i.ImageUrl,
            Title             = i.Title,
            AltText           = i.AltText,
            DisplayOrder      = i.DisplayOrder,
            IsActive          = i.IsActive
        }).ToList()
    };
}
