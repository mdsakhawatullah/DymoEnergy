using Volo.Abp.Application.Dtos;

namespace DymoEnergy.Categories;

public class CategoryFilterDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsFeatured  { get; set; }
    public int? PortalId    { get; set; }
}
