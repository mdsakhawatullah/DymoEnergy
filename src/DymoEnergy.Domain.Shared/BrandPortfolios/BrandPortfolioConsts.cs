namespace DymoEnergy.BrandPortfolios;

public static class BrandPortfolioConsts
{
    public const int MaxNameLength        = 128;
    public const int MaxSlugLength        = 128;
    public const int MaxTaglineLength     = 256;
    public const int MaxUrlLength         = 1024;
    public const int MaxShortTextLength   = 256;
    public const int MaxIntroLength       = 2000;
    public const int MaxColorLength       = 32;
    public const int MaxIconKeyLength     = 64;
    public const int MaxFileSizeLength    = 64;
    public const int MaxCertNumberLength  = 128;
    public const int MaxMetaTitleLength   = 256;
    public const int MaxMetaDescLength    = 512;

    /// <summary>Story paragraphs, quick stats, facts, reasons and warranty rows are kept as one JSON document.</summary>
    public const int MaxContentJsonLength = 20000;
}
