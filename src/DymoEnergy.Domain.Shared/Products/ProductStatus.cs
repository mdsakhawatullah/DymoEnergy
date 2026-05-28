namespace DymoEnergy.Products;

/// <summary>
/// Lifecycle status of a <see cref="Product"/> in the storefront.
/// </summary>
public enum ProductStatus
{
    /// <summary>Saved but not yet submitted for review or publishing.</summary>
    Draft = 1,

    /// <summary>Visible and purchasable on the storefront.</summary>
    Active = 2,

    /// <summary>Hidden from the storefront but preserved for future use.</summary>
    Inactive = 3,

    /// <summary>Visible but cannot be purchased — inventory is zero.</summary>
    OutOfStock = 4,

    /// <summary>Permanently retired; retained for order history references only.</summary>
    Discontinued = 5
}
