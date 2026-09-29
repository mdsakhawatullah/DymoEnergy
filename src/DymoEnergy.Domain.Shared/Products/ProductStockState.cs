namespace DymoEnergy.Products;

/// <summary>Stock band used to filter the product list.</summary>
public enum ProductStockState
{
    /// <summary>More than <see cref="ProductConsts.LowStockThreshold"/> units.</summary>
    InStock = 1,

    /// <summary>Between 1 and <see cref="ProductConsts.LowStockThreshold"/> units.</summary>
    LowStock = 2,

    /// <summary>No units left.</summary>
    OutOfStock = 3,
}
