namespace DymoEnergy.Products;

public class CreateUpdateProductImageDto
{
    public int?    Id           { get; set; }
    public string? ImageUrl     { get; set; }
    public string? Title        { get; set; }
    public string? AltText      { get; set; }
    public int     DisplayOrder { get; set; }
    public bool    IsActive     { get; set; }
}
