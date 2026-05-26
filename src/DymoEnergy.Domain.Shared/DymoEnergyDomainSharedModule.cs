using DymoEnergy.Localization;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Validation.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.OpenIddict;
using Volo.Abp.BlobStoring.Database;

namespace DymoEnergy;

[DependsOn(
    typeof(AbpAuditLoggingDomainSharedModule),
    typeof(AbpBackgroundJobsDomainSharedModule),
    typeof(AbpFeatureManagementDomainSharedModule),
    typeof(AbpPermissionManagementDomainSharedModule),
    typeof(AbpSettingManagementDomainSharedModule),
    typeof(AbpIdentityDomainSharedModule),
    typeof(AbpOpenIddictDomainSharedModule),
    typeof(BlobStoringDatabaseDomainSharedModule)
    )]
public class DymoEnergyDomainSharedModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        DymoEnergyGlobalFeatureConfigurator.Configure();
        DymoEnergyModuleExtensionConfigurator.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<DymoEnergyDomainSharedModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<DymoEnergyResource>("zh-Hant")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("/Localization/DymoEnergy");

            options.DefaultResourceType = typeof(DymoEnergyResource);
            
            options.Languages.Add(new LanguageInfo("zh-Hant", "zh-Hant", "Chinese (Traditional)")); 
            options.Languages.Add(new LanguageInfo("en-GB", "en-GB", "English (UK)")); 
            options.Languages.Add(new LanguageInfo("is", "is", "Icelandic")); 
            options.Languages.Add(new LanguageInfo("bn", "bn", "Bangla")); 

        });
        
        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("DymoEnergy", typeof(DymoEnergyResource));
        });
    }
}
