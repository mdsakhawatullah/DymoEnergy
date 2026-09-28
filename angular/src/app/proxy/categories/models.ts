export interface CategoryDto {
  id: number;
  name?: string;
  slug?: string;
  description?: string;
  longDescription?: string;
  heroTitle?: string;
  heroSubtitle?: string;
  heroCtaText?: string;
  heroCtaUrl?: string;
  primaryBackgroundImageUrl?: string;
  thumbnailImageUrl?: string;
  overlayColor?: string;
  overlayOpacity: number;
  primaryTextColor?: string;
  accentColor?: string;
  sectionBackgroundColor?: string;
  layoutType: CategoryLayoutType;
  isPublished: boolean;
  isFeatured: boolean;
  displayOrder: number;
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
  creationTime?: string;
  lastModificationTime?: string;
  images: CategoryImageDto[];
}

export interface CategoryImageDto {
  id: number;
  categoryId: number;
  imageUrl?: string;
  imageType: number;
  title?: string;
  altText?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface CategoryFilterDto {
  filter?: string;
  isPublished?: boolean;
  isFeatured?: boolean;
  portalId?: number;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export type CategoryLayoutType = 1 | 2 | 3 | 4 | 5;

export const CategoryLayoutTypeLabels: Record<number, string> = {
  1: 'Full Width Hero',
  2: 'Split Hero',
  3: 'Minimalist Hero',
  4: 'Video Hero',
  5: 'Slider Hero',
};

export interface CreateUpdateCategoryDto {
  portalId?: number;
  name?: string;
  slug?: string;
  description?: string;
  longDescription?: string;
  heroTitle?: string;
  heroSubtitle?: string;
  heroCtaText?: string;
  heroCtaUrl?: string;
  primaryBackgroundImageUrl?: string;
  thumbnailImageUrl?: string;
  overlayColor?: string;
  overlayOpacity: number;
  primaryTextColor?: string;
  accentColor?: string;
  sectionBackgroundColor?: string;
  layoutType: CategoryLayoutType;
  isPublished: boolean;
  isFeatured: boolean;
  displayOrder: number;
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
  images: any[];
}

export interface DymoPagedResultDto<T> {
  totalCount: number;
  items: T[];
}

export interface SelectListDto {
  value: number;
  displayText: string;
}
