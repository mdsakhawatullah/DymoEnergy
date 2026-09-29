import { DymoPagedResultDto } from '../categories/models';

export { DymoPagedResultDto };

export interface ProductDto {
  id: number;
  portalId?: number;
  categoryId: number;
  ribbonText?: string;
  name?: string;
  slug?: string;
  summary?: string;
  sku?: string;
  price: number;
  discountPrice?: number;
  weight?: string;
  vendorId?: number;
  vendorCode?: string;
  vendorName?: string;
  category1Id?: number;
  category2Id?: number;
  category3Id?: number;
  bundle: boolean;
  bundleCode?: string;
  status: ProductStatusType;
  isActive: boolean;
  isFeatured: boolean;
  displayOrder: number;
  stockQuantity: number;
  primaryImage?: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
  creationTime?: string;
  lastModificationTime?: string;
  images: ProductImageDto[];
}

export interface ProductImageDto {
  id: number;
  productId: number;
  imageUrl?: string;
  title?: string;
  altText?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface ProductFilterDto {
  filter?: string;
  status?: ProductStatusType;
  isActive?: boolean;
  isFeatured?: boolean;
  categoryId?: number;
  portalId?: number;
  /** 1 in stock · 2 low stock · 3 out of stock — mirrors ProductStockState. */
  stockState?: number;
  /** Low / out of stock, or no main photo. */
  needsAttention?: boolean;
  /** newest · oldest · name · price-asc · price-desc · stock-asc */
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface CreateUpdateProductDto {
  portalId?: number;
  categoryId: number;
  ribbonText?: string;
  name?: string;
  slug?: string;
  summary?: string;
  sku?: string;
  price: number;
  discountPrice?: number;
  weight?: string;
  vendorId?: number;
  vendorCode?: string;
  vendorName?: string;
  category1Id?: number;
  category2Id?: number;
  category3Id?: number;
  bundle: boolean;
  bundleCode?: string;
  status: ProductStatusType;
  isActive: boolean;
  isFeatured: boolean;
  displayOrder: number;
  stockQuantity: number;
  primaryImage?: string;
  description?: string;
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
  images: any[];
}

export type ProductStatusType = 1 | 2 | 3 | 4 | 5;

export const ProductStatusLabels: Record<number, string> = {
  1: 'Draft',
  2: 'Active',
  3: 'Inactive',
  4: 'Out of Stock',
  5: 'Discontinued',
};

export const ProductStatusColors: Record<number, string> = {
  1: 'default',
  2: 'success',
  3: 'warning',
  4: 'error',
  5: 'default',
};
