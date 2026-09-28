using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.Categories;

public class CreateUpdateCategoryImageDto
{
    public int? Id { get; set; }

    public string? ImageUrl { get; set; }

    public CategoryImageType ImageType    { get; set; }

    public string? Title { get; set; }

    public string? AltText { get; set; }

    public int  DisplayOrder { get; set; }

    public bool IsActive     { get; set; }
}
