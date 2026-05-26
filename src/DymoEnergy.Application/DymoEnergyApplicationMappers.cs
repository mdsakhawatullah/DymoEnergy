using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using DymoEnergy.Books;
using DymoEnergy.AdminSiteSettings;

namespace DymoEnergy;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class DymoEnergyBookToBookDtoMapper : MapperBase<Book, BookDto>
{
    public override partial BookDto Map(Book source);

    public override partial void Map(Book source, BookDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class DymoEnergyCreateUpdateBookDtoToBookMapper : MapperBase<CreateUpdateBookDto, Book>
{
    public override partial Book Map(CreateUpdateBookDto source);

    public override partial void Map(CreateUpdateBookDto source, Book destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class DymoEnergyAdminSiteSettingToAdminSiteSettingDtoMapper : MapperBase<AdminSiteSetting, AdminSiteSettingDto>
{
    public override partial AdminSiteSettingDto Map(AdminSiteSetting source);

    public override partial void Map(AdminSiteSetting source, AdminSiteSettingDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class DymoEnergyCreateUpdateAdminSiteSettingDtoToAdminSiteSettingMapper : MapperBase<CreateUpdateAdminSiteSettingDto, AdminSiteSetting>
{
    public override partial AdminSiteSetting Map(CreateUpdateAdminSiteSettingDto source);

    public override partial void Map(CreateUpdateAdminSiteSettingDto source, AdminSiteSetting destination);
}
