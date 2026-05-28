namespace DymoEnergy.Catalogues;

public class CatalogueImageDto
{
    public int Id { get; set; }
    public int CatalogueId  { get; set; }
    public string? ImageUrl { get; set; }
    public CatalogueImageType ImageType { get; set; }
    public string? Title { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
