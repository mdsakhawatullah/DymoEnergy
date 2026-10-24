// ── Enums (mirror DymoEnergy.Domain.Shared) ──────────────────────────────

export enum BrandHeroKind {
  Photo = 0,
  Video = 1,
}

export enum BrandDownloadKind {
  Pdf = 0,
  Zip = 1,
  Other = 2,
}

export enum BrandProjectConsentStatus {
  PhotoAndConsent = 0,
  WaitingOnConsent = 1,
  NoPhoto = 2,
}

// ── Child rows ───────────────────────────────────────────────────────────

export interface BrandPortfolioCertificateDto {
  id: number;
  brandPortfolioId: number;
  mark?: string;
  covers?: string;
  certificateNumber?: string;
  noExpiry: boolean;
  expiresOn?: string;
  fileUrl?: string;
  fileSize?: string;
  isVisible: boolean;
  displayOrder: number;
}

export interface BrandPortfolioDownloadDto {
  id: number;
  brandPortfolioId: number;
  title?: string;
  kind: BrandDownloadKind;
  fileUrl?: string;
  fileSize?: string;
  dealersOnly: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface BrandPortfolioProductLineDto {
  id: number;
  brandPortfolioId: number;
  name?: string;
  blurb?: string;
  categoryId?: number;
  iconKey?: string;
  isVisible: boolean;
  displayOrder: number;
  /** Worked out by the server from the linked category. */
  productCount: number;
  /** Cheapest live product in the linked category. */
  fromPrice?: number;
}

export interface BrandPortfolioProjectDto {
  id: number;
  brandPortfolioId: number;
  projectId?: number;
  title?: string;
  subtitle?: string;
  imageUrl?: string;
  consentStatus: BrandProjectConsentStatus;
  displayOrder: number;
}

export interface BrandPortfolioMediaDto {
  id: number;
  brandPortfolioId: number;
  imageUrl?: string;
  caption?: string;
  altText?: string;
  isActive: boolean;
  displayOrder: number;
}

// ── The page ─────────────────────────────────────────────────────────────

export interface BrandPortfolioDto {
  id: number;
  creationTime?: string;
  lastModificationTime?: string;
  portalId?: number;

  name?: string;
  slug?: string;
  tagline?: string;
  logoUrl?: string;
  shortIntro?: string;
  distributorSince?: string;
  brandColor?: string;

  storyHeadline?: string;

  heroKind: BrandHeroKind;
  heroMediaUrl?: string;
  heroCaption?: string;
  heroAutoplayMuted: boolean;
  heroUsedForSharing: boolean;

  ctaHeadline?: string;
  ctaSubline?: string;

  metaTitle?: string;
  metaDescription?: string;

  /** Quick stats, facts, paragraphs, reasons and warranty rows, as one JSON string. */
  contentJson?: string;

  isPublished: boolean;
  isActive: boolean;
  displayOrder: number;

  certificates: BrandPortfolioCertificateDto[];
  downloads: BrandPortfolioDownloadDto[];
  productLines: BrandPortfolioProductLineDto[];
  projects: BrandPortfolioProjectDto[];
  media: BrandPortfolioMediaDto[];
}

export interface BrandPortfolioListDto {
  id: number;
  creationTime?: string;
  lastModificationTime?: string;
  name?: string;
  slug?: string;
  tagline?: string;
  logoUrl?: string;
  brandColor?: string;
  isPublished: boolean;
  isActive: boolean;
  displayOrder: number;
  certificateCount: number;
  productLineCount: number;
  /** 0–100: how much of the page carries something. */
  completenessPercent: number;
}

// ── Create / update ──────────────────────────────────────────────────────

export interface CreateUpdateBrandPortfolioCertificateDto {
  id?: number | null;
  mark?: string;
  covers?: string;
  certificateNumber?: string;
  noExpiry: boolean;
  expiresOn?: string | null;
  fileUrl?: string;
  fileSize?: string;
  isVisible: boolean;
  displayOrder: number;
}

export interface CreateUpdateBrandPortfolioDownloadDto {
  id?: number | null;
  title?: string;
  kind: BrandDownloadKind;
  fileUrl?: string;
  fileSize?: string;
  dealersOnly: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface CreateUpdateBrandPortfolioProductLineDto {
  id?: number | null;
  name?: string;
  blurb?: string;
  categoryId?: number | null;
  iconKey?: string;
  isVisible: boolean;
  displayOrder: number;
}

export interface CreateUpdateBrandPortfolioProjectDto {
  id?: number | null;
  projectId?: number | null;
  title?: string;
  subtitle?: string;
  imageUrl?: string;
  consentStatus: BrandProjectConsentStatus;
  displayOrder: number;
}

export interface CreateUpdateBrandPortfolioMediaDto {
  id?: number | null;
  imageUrl?: string;
  caption?: string;
  altText?: string;
  isActive: boolean;
  displayOrder: number;
}

export interface CreateUpdateBrandPortfolioDto {
  portalId?: number;
  name: string;
  slug: string;
  tagline?: string;
  logoUrl?: string;
  shortIntro?: string;
  distributorSince?: string;
  brandColor?: string;
  storyHeadline?: string;
  heroKind: BrandHeroKind;
  heroMediaUrl?: string;
  heroCaption?: string;
  heroAutoplayMuted: boolean;
  heroUsedForSharing: boolean;
  ctaHeadline?: string;
  ctaSubline?: string;
  metaTitle?: string;
  metaDescription?: string;
  contentJson?: string;
  isPublished: boolean;
  isActive: boolean;
  displayOrder: number;
  certificates: CreateUpdateBrandPortfolioCertificateDto[];
  downloads: CreateUpdateBrandPortfolioDownloadDto[];
  productLines: CreateUpdateBrandPortfolioProductLineDto[];
  projects: CreateUpdateBrandPortfolioProjectDto[];
  media: CreateUpdateBrandPortfolioMediaDto[];
}

export interface PagedBrandPortfolioResultDto {
  totalCount: number;
  items: BrandPortfolioListDto[];
}

export interface ListBrandPortfolioResultDto {
  items: BrandPortfolioListDto[];
}

// ── The narrative blocks kept inside contentJson ─────────────────────────

export interface BrandQuickStat { iconKey: string; value: string; label: string; }
export interface BrandFact { key: string; value: string; }
export interface BrandParagraph { text: string; }
export interface BrandReason { title: string; text: string; }
export interface BrandWarrantyTerm { category: string; term: string; }
export interface BrandWarrantyNote { text: string; }

export interface BrandPortfolioContent {
  quickStats: BrandQuickStat[];
  facts: BrandFact[];
  paragraphs: BrandParagraph[];
  reasons: BrandReason[];
  warrantyTerms: BrandWarrantyTerm[];
  warrantyNotes: BrandWarrantyNote[];
}
