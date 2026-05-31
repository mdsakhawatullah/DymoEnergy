export interface UserSiteSettingImageDto {
  id: number;
  userSiteSettingId: number;
  imageUrl?: string;
  title?: string;
  altText?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface UserSiteSettingDto {
  id: number;
  creationTime?: string;
  lastModificationTime?: string;
  portalId?: number;
  // ── General ──────────────────────────────────────────────────────────
  backgroundImage?: string;
  description?: string;
  // ── Brand colours ─────────────────────────────────────────────────────
  buttonColor?: string;
  primaryColor?: string;
  bodyColor?: string;
  // ── Backgrounds ───────────────────────────────────────────────────────
  backgroundColor?: string;
  cardBgColor?: string;
  // ── Navbar ────────────────────────────────────────────────────────────
  navbarBgColor?: string;
  navbarTextColor?: string;
  // ── Sidebar ───────────────────────────────────────────────────────────
  sidebarBgColor?: string;
  sidebarTextColor?: string;
  sidebarActiveBgColor?: string;
  // ── Buttons ───────────────────────────────────────────────────────────
  buttonPrimaryBgColor?: string;
  buttonPrimaryTextColor?: string;
  // ── Typography ────────────────────────────────────────────────────────
  fontFamily?: string;
  fontSizeBase?: string;
  isActive: boolean;
  images: UserSiteSettingImageDto[];
}

export interface CreateUpdateUserSiteSettingImageDto {
  id?: number;
  imageUrl?: string;
  title?: string;
  altText?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateUpdateUserSiteSettingDto {
  portalId?: number;
  backgroundImage?: string;
  description?: string;
  buttonColor?: string;
  primaryColor?: string;
  bodyColor?: string;
  backgroundColor?: string;
  cardBgColor?: string;
  navbarBgColor?: string;
  navbarTextColor?: string;
  sidebarBgColor?: string;
  sidebarTextColor?: string;
  sidebarActiveBgColor?: string;
  buttonPrimaryBgColor?: string;
  buttonPrimaryTextColor?: string;
  fontFamily?: string;
  fontSizeBase?: string;
  isActive: boolean;
  images: CreateUpdateUserSiteSettingImageDto[];
}

export interface PagedUserSiteSettingResultDto {
  totalCount: number;
  items: UserSiteSettingDto[];
}
