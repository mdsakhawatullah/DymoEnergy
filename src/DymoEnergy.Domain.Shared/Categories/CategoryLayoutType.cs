namespace DymoEnergy.Categories;

/// <summary>
/// Controls which hero / page layout the front-end renders for this category.
/// </summary>
public enum CategoryLayoutType
{
    /// <summary>Classic 100 % viewport-height background image with centred text.</summary>
    FullWidthHero  = 1,

    /// <summary>50/50 split: image on one side, text on the other.</summary>
    SplitHero      = 2,

    /// <summary>Minimal top banner with a small headline; content leads below.</summary>
    MinimalistHero = 3,

    /// <summary>Looping background video with overlaid text (video URL stored in HeroCtaUrl).</summary>
    VideoHero      = 4,

    /// <summary>Auto-play image carousel built from SecondaryBackground images.</summary>
    SliderHero     = 5
}
