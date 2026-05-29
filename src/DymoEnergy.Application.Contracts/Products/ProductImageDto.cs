namespace DymoEnergy.Products;

public class ProductImageDto
{
    public int     Id           { get; set; }
    public int     ProductId    { get; set; }
    public string? ImageUrl     { get; set; }
    public string? Title        { get; set; }
    public string? AltText      { get; set; }
    public int     DisplayOrder { get; set; }
    public bool    IsActive     { get; set; }
}
