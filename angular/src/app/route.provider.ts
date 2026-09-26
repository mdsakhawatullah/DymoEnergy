import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

/** Marker icon class picked up by CSS to render an entry as a section label. */
const SECTION = 'de-section-label';

const APP = eLayoutType.application;

/**
 * Sidebar structure follows reference/Sidebar.dc.html: five uppercase sections,
 * mostly flat leaves, with only a few expandable groups.
 *
 * Deviation from the mock: Sales and Inventory & Procurement stay expandable
 * because their children are real, working pages. Collapsing them to a single
 * "coming soon" leaf, as the mock shows, would hide shipped features.
 */
function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([

    // ══ OVERVIEW ════════════════════════════════════════════════════════════
    { path: 'section-overview', name: '::Menu:SectionOverview', iconClass: SECTION, order: 1, layout: APP },
    {
      path: '/',
      name: '::Menu:Dashboard',
      iconClass: 'bi bi-speedometer2',
      order: 2,
      layout: APP,
      breadcrumbText: '::Menu:Dashboard',
    },
    {
      path: '/dashboard',
      name: '::Menu:AnalyticsReporting',
      iconClass: 'bi bi-bar-chart',
      order: 3,
      layout: APP,
      breadcrumbText: '::Menu:AnalyticsReporting',
    },

    // ══ COMMERCE ════════════════════════════════════════════════════════════
    { path: 'section-commerce', name: '::Menu:SectionCommerce', iconClass: SECTION, order: 10, layout: APP },

    { path: '', name: '::Menu:ECommerce', iconClass: 'bi bi-bag', order: 11, layout: APP },
    {
      path: '/orders',
      name: '::Menu:Orders',
      parentName: '::Menu:ECommerce',
      order: 1,
      layout: APP,
      breadcrumbText: '::Menu:Orders',
    },
    {
      path: '/quote-requests',
      name: '::Menu:QuoteRequests',
      parentName: '::Menu:ECommerce',
      order: 2,
      layout: APP,
      breadcrumbText: '::Menu:QuoteRequests',
    },

    { path: '', name: '::Menu:Sales', iconClass: 'bi bi-cart', order: 12, layout: APP },
    { path: '/point-of-sales', name: '::Menu:PointOfSales', parentName: '::Menu:Sales', order: 1, layout: APP, breadcrumbText: '::Menu:PointOfSales' },
    { path: '/pos-counters',   name: '::Menu:POSCounters',  parentName: '::Menu:Sales', order: 2, layout: APP, breadcrumbText: '::Menu:POSCounters' },
    { path: '/sales-invoices', name: '::Menu:SalesInvoice', parentName: '::Menu:Sales', order: 3, layout: APP, breadcrumbText: '::Menu:SalesInvoice' },
    { path: '/product-returns', name: '::Menu:ProductReturn', parentName: '::Menu:Sales', order: 4, layout: APP, breadcrumbText: '::Menu:ProductReturn' },
    { path: '/promotions',     name: '::Menu:Promotion',    parentName: '::Menu:Sales', order: 5, layout: APP, breadcrumbText: '::Menu:Promotion' },
    { path: '/customers',      name: '::Menu:Customer',     parentName: '::Menu:Sales', order: 6, layout: APP, breadcrumbText: '::Menu:Customer' },
    { path: '/catalogues',     name: '::Menu:Catalogue',    parentName: '::Menu:Sales', order: 7, layout: APP, breadcrumbText: '::Menu:Catalogue' },

    {
      path: '/marketing-overview',
      name: '::Menu:Marketing',
      iconClass: 'bi bi-megaphone',
      order: 13,
      layout: APP,
      breadcrumbText: '::Menu:Marketing',
    },
    {
      path: '/shipping-setup',
      name: '::Menu:ShippingCourier',
      iconClass: 'bi bi-truck',
      order: 14,
      layout: APP,
      breadcrumbText: '::Menu:ShippingCourier',
    },

    // ══ OPERATIONS ══════════════════════════════════════════════════════════
    { path: 'section-operations', name: '::Menu:SectionOperations', iconClass: SECTION, order: 20, layout: APP },

    {
      path: '/projects',
      name: '::Menu:ProjectPlanning',
      iconClass: 'bi bi-list-task',
      order: 21,
      layout: APP,
      breadcrumbText: '::Menu:ProjectPlanning',
    },

    { path: '', name: '::Menu:AssetSiteManagement', iconClass: 'bi bi-grid-3x3-gap', order: 22, layout: APP },
    { path: '/sites',  name: '::Menu:Sites',  parentName: '::Menu:AssetSiteManagement', order: 1, layout: APP, breadcrumbText: '::Menu:Sites' },
    { path: '/assets', name: '::Menu:Assets', parentName: '::Menu:AssetSiteManagement', order: 2, layout: APP, breadcrumbText: '::Menu:Assets' },
    { path: '/preventive-maintenance', name: '::Menu:PreventiveMaintenance', parentName: '::Menu:AssetSiteManagement', order: 3, layout: APP, breadcrumbText: '::Menu:PreventiveMaintenance' },
    { path: '/asset-performance',      name: '::Menu:AssetPerformance',      parentName: '::Menu:AssetSiteManagement', order: 4, layout: APP, breadcrumbText: '::Menu:AssetPerformance' },

    {
      path: '/inspections',
      name: '::Menu:SiteInspections',
      iconClass: 'bi bi-clipboard-check',
      order: 23,
      layout: APP,
      breadcrumbText: '::Menu:SiteInspections',
    },
    {
      path: '/maintenance-plans',
      name: '::Menu:MaintenanceService',
      iconClass: 'bi bi-wrench',
      order: 24,
      layout: APP,
      breadcrumbText: '::Menu:MaintenanceService',
    },
    {
      path: '/tasks',
      name: '::Menu:WorkforceTaskManagement',
      iconClass: 'bi bi-people',
      order: 25,
      layout: APP,
      breadcrumbText: '::Menu:WorkforceTaskManagement',
    },

    { path: '', name: '::Menu:InventoryProcurement', iconClass: 'bi bi-box-seam', order: 26, layout: APP },
    { path: '/products',       name: '::Menu:Product',       parentName: '::Menu:InventoryProcurement', order: 1, layout: APP, breadcrumbText: '::Menu:Product' },
    { path: '/stock-entries',  name: '::Menu:StockEntry',    parentName: '::Menu:InventoryProcurement', order: 2, layout: APP, breadcrumbText: '::Menu:StockEntry' },
    { path: '/stock-ledger',   name: '::Menu:StockLedger',   parentName: '::Menu:InventoryProcurement', order: 3, layout: APP, breadcrumbText: '::Menu:StockLedger' },
    { path: '/suppliers',      name: '::Menu:Suppliers',     parentName: '::Menu:InventoryProcurement', order: 4, layout: APP, breadcrumbText: '::Menu:Suppliers' },
    { path: '/purchase-orders', name: '::Menu:PurchaseOrders', parentName: '::Menu:InventoryProcurement', order: 5, layout: APP, breadcrumbText: '::Menu:PurchaseOrders' },
    { path: '/quotes-deliveries', name: '::Menu:QuotesDeliveries', parentName: '::Menu:InventoryProcurement', order: 6, layout: APP, breadcrumbText: '::Menu:QuotesDeliveries' },

    // ══ FINANCE ═════════════════════════════════════════════════════════════
    { path: 'section-finance', name: '::Menu:SectionFinance', iconClass: SECTION, order: 30, layout: APP },
    {
      path: '/budgets-costs',
      name: '::Menu:FinancialsBilling',
      iconClass: 'bi bi-wallet2',
      order: 31,
      layout: APP,
      breadcrumbText: '::Menu:FinancialsBilling',
    },
    {
      path: '/compliance-documents',
      name: '::Menu:ComplianceDocumentation',
      iconClass: 'bi bi-shield-check',
      order: 32,
      layout: APP,
      breadcrumbText: '::Menu:ComplianceDocumentation',
    },

    // ══ SETUP ═══════════════════════════════════════════════════════════════
    { path: 'section-setup', name: '::Menu:SectionSetup', iconClass: SECTION, order: 40, layout: APP },

    { path: '', name: '::Menu:Companies', iconClass: 'bi bi-building', order: 41, layout: APP },
    {
      path: '/companies',
      name: '::Menu:Company',
      parentName: '::Menu:Companies',
      order: 1,
      layout: APP,
      breadcrumbText: '::Menu:Company',
    },

    { path: '', name: '::Menu:SiteSettings', iconClass: 'bi bi-sliders', order: 42, layout: APP },
    {
      path: '/admin-site-settings',
      name: '::Menu:AdminSiteSetting',
      parentName: '::Menu:SiteSettings',
      order: 1,
      layout: APP,
      breadcrumbText: '::Menu:AdminSiteSetting',
    },
    {
      path: '/user-site-settings',
      name: '::Menu:UserSiteSetting',
      parentName: '::Menu:SiteSettings',
      order: 2,
      layout: APP,
      breadcrumbText: '::Menu:UserSiteSetting',
    },
  ]);
}
