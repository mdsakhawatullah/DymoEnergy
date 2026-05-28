namespace DymoEnergy.Catalogues;

/// <summary>
/// Describes the role of an image attached to a <see cref="Catalogue"/>.
/// Admin can upload one or many images of each type to build a rich page.
/// </summary>
public enum CatalogueImageType
{
    /// <summary>The main full-width hero background shown at the top of the page.</summary>
    PrimaryBackground  = 1,

    /// <summary>Additional background images used in parallax or multi-section layouts.</summary>
    SecondaryBackground = 2,

    /// <summary>Overlay / mid-section banner strips.</summary>
    Banner             = 3,

    /// <summary>Images shown inside a gallery / lightbox section.</summary>
    Gallery            = 4,

    /// <summary>Small card thumbnail displayed in catalogue listing grids.</summary>
    Thumbnail          = 5,

    /// <summary>SVG or PNG category icon shown next to the catalogue title.</summary>
    Icon               = 6,

    /// <summary>Feature highlight image placed alongside descriptive text.</summary>
    FeatureImage       = 7
}
