using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.Compliance;

public interface IComplianceAppService : IApplicationService
{
    Task<CompliancePageDto> GetPageAsync();

    Task<ComplianceSettingDto> UpdateSettingAsync(UpdateComplianceSettingDto input);

    Task<ComplianceLicenceDto> CreateLicenceAsync(CreateUpdateComplianceLicenceDto input);
    Task<ComplianceLicenceDto> UpdateLicenceAsync(int id, CreateUpdateComplianceLicenceDto input);
    Task                       DeleteLicenceAsync(int id);

    Task<ComplianceFilingDto> CreateFilingAsync(CreateUpdateComplianceFilingDto input);
    Task<ComplianceFilingDto> UpdateFilingAsync(int id, CreateUpdateComplianceFilingDto input);
    Task<ComplianceFilingDto> UpdateFilingStatusAsync(int id, UpdateComplianceFilingStatusDto input);
    Task                      DeleteFilingAsync(int id);

    Task<ComplianceCertificateDto> CreateCertificateAsync(CreateUpdateComplianceCertificateDto input);
    Task<ComplianceCertificateDto> UpdateCertificateAsync(int id, CreateUpdateComplianceCertificateDto input);
    Task                           DeleteCertificateAsync(int id);

    Task<ComplianceListItemDto> CreateItemAsync(CreateUpdateComplianceListItemDto input);
    Task<ComplianceListItemDto> UpdateItemAsync(int id, CreateUpdateComplianceListItemDto input);
    Task                        DeleteItemAsync(int id);

    Task SetProjectDocumentAsync(SetComplianceProjectDocumentDto input);

    Task<ComplianceFileDto> AddFileAsync(AddComplianceFileDto input);
    Task                    DeleteFileAsync(int id);
}
