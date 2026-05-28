namespace DymoEnergy.Catalogues;

public static class CatalogueConsts
{
    public const int MaxNameLength             = 128;
    public const int MaxSlugLength             = 128;
    public const int MaxDescriptionLength      = 512;
    public const int MaxLongDescriptionLength  = 65_535;

    // Hero section
    public const int MaxHeroTitleLength     = 256;
    public const int MaxHeroSubtitleLength  = 512;
    public const int MaxHeroCtaTextLength   = 64;
    public const int MaxHeroCtaUrlLength    = 512;

    // Images
    public const int MaxImageUrlLength   = 1_024;
    public const int MaxImageTitleLength = 256;
    public const int MaxAltTextLength    = 256;

    // Colours / styling
    public const int MaxColorLength = 32;   // e.g. "#RRGGBBAA" or CSS var

    // SEO
    public const int MaxMetaTitleLength       = 256;
    public const int MaxMetaDescriptionLength = 512;
    public const int MaxMetaKeywordsLength    = 512;
}
