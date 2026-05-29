export interface AdminSiteSettingDto {
  id: number;
  creationTime?: string;
  lastModificationTime?: string;
  siteName?: string;
  buttonColor?: string;
  tagline?: string;
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  accentColor?: string;
  textColor?: string;
  textMutedColor?: string;
  linkColor?: string;
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
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  phone?: string;
  email?: string;
  supportEmail?: string;
  whatsApp?: string;
}

export interface CreateUpdateAdminSiteSettingDto {
  siteName?: string;
  buttonColor?: string;
  tagline?: string;
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  accentColor?: string;
  textColor?: string;
  textMutedColor?: string;
  linkColor?: string;
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
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  phone?: string;
  email?: string;
  supportEmail?: string;
  whatsApp?: string;
}

export interface PagedAdminSiteSettingResultDto {
  totalCount: number;
  items: AdminSiteSettingDto[];
}
