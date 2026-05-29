using Volo.Abp.Application.Dtos;

namespace DymoEnergy.Products;

public class ProductFilterDto : PagedAndSortedResultRequestDto
{
    public string?        Filter      { get; set; }
    public ProductStatus? Status      { get; set; }
    public bool?          IsActive    { get; set; }
    public bool?          IsFeatured  { get; set; }
    public int?           CatalogueId { get; set; }
    public int?           PortalId    { get; set; }
}
