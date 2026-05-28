-- ============================================================
--  DymoEnergy — Catalogue Seed Data (5 Solar Product Types)
--  Tables  : DymoCatalogues, DymoCatalogueImages
--  Run on  : SQL Server (after running EF migrations)
-- ============================================================
--
--  CatalogueLayoutType   CatalogueImageType
--  ─────────────────     ──────────────────
--  1  FullWidthHero      1  PrimaryBackground
--  2  SplitHero          2  SecondaryBackground
--  3  MinimalistHero     3  Banner
--  4  VideoHero          4  Gallery
--  5  SliderHero         5  Thumbnail
--                        6  Icon
--                        7  FeatureImage
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- Safety: remove existing seed rows so script is re-runnable
DELETE FROM DymoCatalogueImages
WHERE CatalogueId IN (SELECT Id FROM DymoCatalogues WHERE Slug IN (
    'solar-panels','solar-inverters','battery-storage',
    'complete-solar-kits','solar-accessories'));

DELETE FROM DymoCatalogues WHERE Slug IN (
    'solar-panels','solar-inverters','battery-storage',
    'complete-solar-kits','solar-accessories');

DECLARE @Id1 INT, @Id2 INT, @Id3 INT, @Id4 INT, @Id5 INT;

-- ============================================================
-- 1. SOLAR PANELS
-- ============================================================
INSERT INTO DymoCatalogues (
    Name, Slug, PortalId, Description, LongDescription,
    HeroTitle, HeroSubtitle, HeroCtaText, HeroCtaUrl,
    PrimaryBackgroundImageUrl, ThumbnailImageUrl,
    OverlayColor, OverlayOpacity, PrimaryTextColor, AccentColor, SectionBackgroundColor,
    LayoutType, IsPublished, IsFeatured, DisplayOrder,
    MetaTitle, MetaDescription, MetaKeywords,
    ExtraProperties, ConcurrencyStamp, CreationTime, IsDeleted
)
VALUES (
    'Solar Panels',
    'solar-panels',
    NULL,
    'High-efficiency monocrystalline & polycrystalline solar panels for homes and businesses.',
    '<p>Harness the power of the sun with our premium range of solar panels. Whether you need a small residential system or a large commercial installation, we have the right panel for you. All panels come with a 25-year performance warranty and are certified to the highest international standards.</p>',
    'Power Your Home With the Sun',
    'High-efficiency solar panels from 400W to 700W. Cut your electricity bill by up to 90%.',
    'Shop Solar Panels',
    '/catalogues/solar-panels',
    'https://images.unsplash.com/photo-1509391366360-2e959784a276?auto=format&fit=crop&w=1920&q=80',
    'https://images.unsplash.com/photo-1509391366360-2e959784a276?auto=format&fit=crop&w=600&q=80',
    '#0F172A', 0.45, '#FFFFFF', '#F59E0B', '#F8FAFC',
    1, 1, 1, 1,
    'Solar Panels – DymoEnergy | High-Efficiency PV Modules',
    'Shop monocrystalline and polycrystalline solar panels. Best prices, 25-year warranty.',
    'solar panels, monocrystalline, polycrystalline, photovoltaic, PV modules',
    '{}', CONVERT(NVARCHAR(40), NEWID()), GETUTCDATE(), 0
);
SET @Id1 = SCOPE_IDENTITY();

INSERT INTO DymoCatalogueImages (CatalogueId, ImageUrl, ImageType, Title, AltText, DisplayOrder, IsActive)
VALUES
    -- PrimaryBackground
    (@Id1, 'https://images.unsplash.com/photo-1509391366360-2e959784a276?auto=format&fit=crop&w=1920&q=80',
     1, 'Solar Panels Hero', 'Rows of solar panels on a rooftop against blue sky', 1, 1),
    -- SecondaryBackground (parallax / section 2)
    (@Id1, 'https://images.unsplash.com/photo-1466611653911-95081537e5b7?auto=format&fit=crop&w=1920&q=80',
     2, 'Solar Field at Sunset', 'Large solar farm at golden hour', 2, 1),
    -- SecondaryBackground (section 3)
    (@Id1, 'https://images.unsplash.com/photo-1497435334941-8c899a9bd4ff?auto=format&fit=crop&w=1920&q=80',
     2, 'Solar Farm Aerial', 'Aerial view of utility-scale solar farm', 3, 1),
    -- Gallery
    (@Id1, 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?auto=format&fit=crop&w=900&q=80',
     4, 'Panel Close-up', 'Close-up of monocrystalline solar cell surface', 1, 1),
    (@Id1, 'https://images.unsplash.com/photo-1508514177221-188b1cf16e9d?auto=format&fit=crop&w=900&q=80',
     4, 'Residential Rooftop', 'Solar panels installed on a residential roof', 2, 1),
    -- Thumbnail
    (@Id1, 'https://images.unsplash.com/photo-1509391366360-2e959784a276?auto=format&fit=crop&w=600&q=80',
     5, 'Solar Panels Card', 'Solar panels – catalogue card thumbnail', 1, 1),
    -- FeatureImage
    (@Id1, 'https://images.unsplash.com/photo-1548668537-07b4f24b7654?auto=format&fit=crop&w=900&q=80',
     7, 'Panel Installation', 'Technician installing solar panels on a roof', 1, 1);


-- ============================================================
-- 2. SOLAR INVERTERS
-- ============================================================
INSERT INTO DymoCatalogues (
    Name, Slug, PortalId, Description, LongDescription,
    HeroTitle, HeroSubtitle, HeroCtaText, HeroCtaUrl,
    PrimaryBackgroundImageUrl, ThumbnailImageUrl,
    OverlayColor, OverlayOpacity, PrimaryTextColor, AccentColor, SectionBackgroundColor,
    LayoutType, IsPublished, IsFeatured, DisplayOrder,
    MetaTitle, MetaDescription, MetaKeywords,
    ExtraProperties, ConcurrencyStamp, CreationTime, IsDeleted
)
VALUES (
    'Solar Inverters',
    'solar-inverters',
    NULL,
    'String, micro, and hybrid inverters to convert solar DC power into usable AC electricity.',
    '<p>An inverter is the brain of your solar system. It converts the direct current (DC) generated by your panels into the alternating current (AC) used by your appliances. We stock string inverters, microinverters, and hybrid inverters with WiFi monitoring from leading brands.</p>',
    'The Smart Heart of Your Solar System',
    'String, micro & hybrid inverters with real-time WiFi monitoring. Up to 98.6% efficiency.',
    'Browse Inverters',
    '/catalogues/solar-inverters',
    'https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1920&q=80',
    'https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=600&q=80',
    '#1E293B', 0.55, '#FFFFFF', '#3B82F6', '#EFF6FF',
    2, 1, 1, 2,
    'Solar Inverters – DymoEnergy | String, Micro & Hybrid',
    'Shop solar inverters including string, micro and hybrid models with WiFi monitoring.',
    'solar inverter, string inverter, microinverter, hybrid inverter, solar converter',
    '{}', CONVERT(NVARCHAR(40), NEWID()), GETUTCDATE(), 0
);
SET @Id2 = SCOPE_IDENTITY();

INSERT INTO DymoCatalogueImages (CatalogueId, ImageUrl, ImageType, Title, AltText, DisplayOrder, IsActive)
VALUES
    -- PrimaryBackground
    (@Id2, 'https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=1920&q=80',
     1, 'Inverter Hero', 'Modern solar inverter mounted on wall', 1, 1),
    -- SecondaryBackground
    (@Id2, 'https://images.unsplash.com/photo-1593941707882-a5bba14938c7?auto=format&fit=crop&w=1920&q=80',
     2, 'Electrical Installation', 'Professional electrical panel installation', 2, 1),
    -- Gallery
    (@Id2, 'https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=900&q=80',
     4, 'Inverter Front', 'Frontal view of a solar string inverter', 1, 1),
    (@Id2, 'https://images.unsplash.com/photo-1593941707882-a5bba14938c7?auto=format&fit=crop&w=900&q=80',
     4, 'Wiring Detail', 'Close-up of inverter DC input wiring', 2, 1),
    -- Banner
    (@Id2, 'https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?auto=format&fit=crop&w=1920&q=80',
     3, 'Energy Banner', 'Clean energy technology banner', 1, 1),
    -- Thumbnail
    (@Id2, 'https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=600&q=80',
     5, 'Inverter Card', 'Solar inverter catalogue card thumbnail', 1, 1);


-- ============================================================
-- 3. BATTERY STORAGE
-- ============================================================
INSERT INTO DymoCatalogues (
    Name, Slug, PortalId, Description, LongDescription,
    HeroTitle, HeroSubtitle, HeroCtaText, HeroCtaUrl,
    PrimaryBackgroundImageUrl, ThumbnailImageUrl,
    OverlayColor, OverlayOpacity, PrimaryTextColor, AccentColor, SectionBackgroundColor,
    LayoutType, IsPublished, IsFeatured, DisplayOrder,
    MetaTitle, MetaDescription, MetaKeywords,
    ExtraProperties, ConcurrencyStamp, CreationTime, IsDeleted
)
VALUES (
    'Battery Storage',
    'battery-storage',
    NULL,
    'Lithium LiFePO4 home and commercial battery systems to store your solar energy 24/7.',
    '<p>Store the excess solar energy you generate during the day and use it at night or during a power outage. Our lithium iron phosphate (LiFePO4) batteries offer over 6,000 charge cycles, 10-year warranty, and seamless integration with all major inverter brands. Go fully off-grid or maximise self-consumption.</p>',
    'Store the Sun. Use It Anytime.',
    'LiFePO4 home batteries from 5 kWh to 100 kWh. Power through outages & peak tariffs.',
    'Explore Battery Storage',
    '/catalogues/battery-storage',
    'https://images.unsplash.com/photo-1620288627223-53302f4e8c74?auto=format&fit=crop&w=1920&q=80',
    'https://images.unsplash.com/photo-1620288627223-53302f4e8c74?auto=format&fit=crop&w=600&q=80',
    '#0C1A2E', 0.50, '#FFFFFF', '#10B981', '#ECFDF5',
    1, 1, 1, 3,
    'Battery Storage – DymoEnergy | LiFePO4 Home & Commercial',
    'Shop LiFePO4 solar battery storage systems. 6,000+ cycles, 10-year warranty.',
    'solar battery, LiFePO4, energy storage, home battery, off-grid battery',
    '{}', CONVERT(NVARCHAR(40), NEWID()), GETUTCDATE(), 0
);
SET @Id3 = SCOPE_IDENTITY();

INSERT INTO DymoCatalogueImages (CatalogueId, ImageUrl, ImageType, Title, AltText, DisplayOrder, IsActive)
VALUES
    -- PrimaryBackground
    (@Id3, 'https://images.unsplash.com/photo-1620288627223-53302f4e8c74?auto=format&fit=crop&w=1920&q=80',
     1, 'Battery Storage Hero', 'Wall-mounted home energy storage battery', 1, 1),
    -- SecondaryBackground
    (@Id3, 'https://images.unsplash.com/photo-1474464955051-3c8db40e3b4a?auto=format&fit=crop&w=1920&q=80',
     2, 'Energy Flow Background', 'Abstract electric energy flow background', 2, 1),
    -- Gallery
    (@Id3, 'https://images.unsplash.com/photo-1620288627223-53302f4e8c74?auto=format&fit=crop&w=900&q=80',
     4, 'Home Battery Unit', 'Sleek white home battery storage unit', 1, 1),
    (@Id3, 'https://images.unsplash.com/photo-1574770118700-4ed7dae3310e?auto=format&fit=crop&w=900&q=80',
     4, 'Battery Cells', 'Lithium battery cell stack close-up', 2, 1),
    -- FeatureImage
    (@Id3, 'https://images.unsplash.com/photo-1611365892117-00ac5ef43c90?auto=format&fit=crop&w=900&q=80',
     7, 'Solar + Storage', 'Solar panels combined with battery storage system', 1, 1),
    -- Thumbnail
    (@Id3, 'https://images.unsplash.com/photo-1620288627223-53302f4e8c74?auto=format&fit=crop&w=600&q=80',
     5, 'Battery Card', 'Battery storage catalogue card thumbnail', 1, 1);


-- ============================================================
-- 4. COMPLETE SOLAR KITS
-- ============================================================
INSERT INTO DymoCatalogues (
    Name, Slug, PortalId, Description, LongDescription,
    HeroTitle, HeroSubtitle, HeroCtaText, HeroCtaUrl,
    PrimaryBackgroundImageUrl, ThumbnailImageUrl,
    OverlayColor, OverlayOpacity, PrimaryTextColor, AccentColor, SectionBackgroundColor,
    LayoutType, IsPublished, IsFeatured, DisplayOrder,
    MetaTitle, MetaDescription, MetaKeywords,
    ExtraProperties, ConcurrencyStamp, CreationTime, IsDeleted
)
VALUES (
    'Complete Solar Kits',
    'complete-solar-kits',
    NULL,
    'All-in-one solar packages — panels, inverter, battery & mounting. Ready to install.',
    '<p>Not sure where to start? Our complete solar kits take the guesswork out of going solar. Each kit includes perfectly matched solar panels, a hybrid inverter, a battery storage unit, and all necessary mounting hardware and cables. Available in 3 kW, 5 kW, 8 kW, and 10 kW configurations for homes, farms, and small businesses.</p>',
    'Everything You Need. One Box.',
    'Complete solar kits from 3 kW to 10 kW. Panels + Inverter + Battery + Mounting included.',
    'View All Kits',
    '/catalogues/complete-solar-kits',
    'https://images.unsplash.com/photo-1582902281572-55aa1d2c76c5?auto=format&fit=crop&w=1920&q=80',
    'https://images.unsplash.com/photo-1582902281572-55aa1d2c76c5?auto=format&fit=crop&w=600&q=80',
    '#134E1F', 0.40, '#FFFFFF', '#22C55E', '#F0FDF4',
    5, 1, 1, 4,
    'Complete Solar Kits – DymoEnergy | 3kW–10kW All-in-One',
    'Buy complete solar kits including panels, inverter, battery and mounting. Easy installation.',
    'solar kit, complete solar system, off-grid kit, solar package, all-in-one solar',
    '{}', CONVERT(NVARCHAR(40), NEWID()), GETUTCDATE(), 0
);
SET @Id4 = SCOPE_IDENTITY();

INSERT INTO DymoCatalogueImages (CatalogueId, ImageUrl, ImageType, Title, AltText, DisplayOrder, IsActive)
VALUES
    -- PrimaryBackground (SliderHero — 3 slides)
    (@Id4, 'https://images.unsplash.com/photo-1582902281572-55aa1d2c76c5?auto=format&fit=crop&w=1920&q=80',
     1, 'Kit Install Hero', 'Solar installation team on rooftop', 1, 1),
    (@Id4, 'https://images.unsplash.com/photo-1611365892117-00ac5ef43c90?auto=format&fit=crop&w=1920&q=80',
     2, 'Green Energy Slide', 'Solar panels on green countryside', 2, 1),
    (@Id4, 'https://images.unsplash.com/photo-1497435334941-8c899a9bd4ff?auto=format&fit=crop&w=1920&q=80',
     2, 'Solar Farm Slide', 'Large solar farm in open landscape', 3, 1),
    -- Gallery
    (@Id4, 'https://images.unsplash.com/photo-1548668537-07b4f24b7654?auto=format&fit=crop&w=900&q=80',
     4, 'Kit Unboxing', 'Components of a complete solar kit laid out', 1, 1),
    (@Id4, 'https://images.unsplash.com/photo-1508514177221-188b1cf16e9d?auto=format&fit=crop&w=900&q=80',
     4, 'Completed Install', 'Finished residential solar system installation', 2, 1),
    -- Banner
    (@Id4, 'https://images.unsplash.com/photo-1582902281572-55aa1d2c76c5?auto=format&fit=crop&w=1920&q=80',
     3, 'Promo Banner', 'Complete solar kit promotional banner', 1, 1),
    -- Thumbnail
    (@Id4, 'https://images.unsplash.com/photo-1582902281572-55aa1d2c76c5?auto=format&fit=crop&w=600&q=80',
     5, 'Kit Card', 'Complete solar kit catalogue card thumbnail', 1, 1);


-- ============================================================
-- 5. SOLAR ACCESSORIES
-- ============================================================
INSERT INTO DymoCatalogues (
    Name, Slug, PortalId, Description, LongDescription,
    HeroTitle, HeroSubtitle, HeroCtaText, HeroCtaUrl,
    PrimaryBackgroundImageUrl, ThumbnailImageUrl,
    OverlayColor, OverlayOpacity, PrimaryTextColor, AccentColor, SectionBackgroundColor,
    LayoutType, IsPublished, IsFeatured, DisplayOrder,
    MetaTitle, MetaDescription, MetaKeywords,
    ExtraProperties, ConcurrencyStamp, CreationTime, IsDeleted
)
VALUES (
    'Solar Accessories',
    'solar-accessories',
    NULL,
    'Mounting systems, MC4 connectors, DC cables, combiner boxes, and monitoring tools.',
    '<p>Complete your solar installation with the right accessories. From aluminium roof-mounting rails and tilt frames to MC4 connectors, 6 mm² DC solar cables, string combiner boxes, and weather-proof junction boxes — we stock everything a certified installer or a confident DIY-er needs. All products are UV and weather rated for 25+ years outdoor use.</p>',
    'Every Component. One Supplier.',
    'Mounting rails, MC4 connectors, DC cables, combiner boxes & more. Next-day delivery.',
    'Shop Accessories',
    '/catalogues/solar-accessories',
    'https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?auto=format&fit=crop&w=1920&q=80',
    'https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?auto=format&fit=crop&w=600&q=80',
    '#1C1917', 0.50, '#FFFFFF', '#F97316', '#FFF7ED',
    3, 1, 0, 5,
    'Solar Accessories – DymoEnergy | Mounting, Cables & Connectors',
    'Shop solar mounting systems, MC4 connectors, DC cables and combiner boxes. Fast delivery.',
    'solar mounting, MC4 connector, solar cable, combiner box, solar accessories',
    '{}', CONVERT(NVARCHAR(40), NEWID()), GETUTCDATE(), 0
);
SET @Id5 = SCOPE_IDENTITY();

INSERT INTO DymoCatalogueImages (CatalogueId, ImageUrl, ImageType, Title, AltText, DisplayOrder, IsActive)
VALUES
    -- PrimaryBackground (MinimalistHero — single banner)
    (@Id5, 'https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?auto=format&fit=crop&w=1920&q=80',
     1, 'Accessories Hero', 'Solar panel mounting hardware and tools on rooftop', 1, 1),
    -- SecondaryBackground
    (@Id5, 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?auto=format&fit=crop&w=1920&q=80',
     2, 'Panel Detail BG', 'Detailed view of solar panel frame and mounting clip', 2, 1),
    -- Gallery
    (@Id5, 'https://images.unsplash.com/photo-1548668537-07b4f24b7654?auto=format&fit=crop&w=900&q=80',
     4, 'Mounting Rails', 'Aluminium mounting rail system on tile roof', 1, 1),
    (@Id5, 'https://images.unsplash.com/photo-1593941707882-a5bba14938c7?auto=format&fit=crop&w=900&q=80',
     4, 'MC4 Connectors', 'MC4 solar cable connector pair', 2, 1),
    (@Id5, 'https://images.unsplash.com/photo-1621905251189-08b45d6a269e?auto=format&fit=crop&w=900&q=80',
     4, 'Combiner Box', 'DC string combiner box with fuses installed', 3, 1),
    -- FeatureImage
    (@Id5, 'https://images.unsplash.com/photo-1582902281572-55aa1d2c76c5?auto=format&fit=crop&w=900&q=80',
     7, 'Install Feature', 'Solar accessories being used during panel installation', 1, 1),
    -- Thumbnail
    (@Id5, 'https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?auto=format&fit=crop&w=600&q=80',
     5, 'Accessories Card', 'Solar accessories catalogue card thumbnail', 1, 1);


-- ============================================================
COMMIT TRANSACTION;

-- Verify
SELECT
    c.Id,
    c.Name,
    c.Slug,
    c.LayoutType,
    c.IsPublished,
    c.IsFeatured,
    c.DisplayOrder,
    COUNT(i.Id) AS ImageCount
FROM DymoCatalogues   c
LEFT JOIN DymoCatalogueImages i ON i.CatalogueId = c.Id
WHERE c.Slug IN ('solar-panels','solar-inverters','battery-storage',
                 'complete-solar-kits','solar-accessories')
GROUP BY c.Id, c.Name, c.Slug, c.LayoutType, c.IsPublished, c.IsFeatured, c.DisplayOrder
ORDER BY c.DisplayOrder;
