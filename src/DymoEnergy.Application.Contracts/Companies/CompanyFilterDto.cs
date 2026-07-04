using Volo.Abp.Application.Dtos;

namespace DymoEnergy.Companies;

public class CompanyFilterDto : PagedAndSortedResultRequestDto
{
    public string?        Filter          { get; set; }
    public CompanyStatus? Status          { get; set; }
    public bool?          IsParentCompany { get; set; }
    public int?           ParentCompanyId { get; set; }
}
