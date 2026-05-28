import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([

    // ── Overview ─────────────────────────────────────────────────────────────
    {
      path: '/',
      name: '::Menu:Home',
      iconClass: 'fas fa-home',
      order: 1,
      layout: eLayoutType.application,
    },

    // ── Admin Site Settings (group) ──────────────────────────────────────────
    {
      path: '',
      name: '::Menu:AdminSiteSettings',
      iconClass: 'fas fa-sliders-h',
      order: 2,
      layout: eLayoutType.application,
    },
    {
      path: '/admin-site-settings',
      name: '::Menu:AdminSiteSetting',
      parentName: '::Menu:AdminSiteSettings',
      iconClass: 'fas fa-sliders-h',
      order: 1,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:AdminSiteSetting',
    },

    // ── Catalogues (group) ───────────────────────────────────────────────────
    {
      path: '',
      name: '::Menu:Catalogues',
      iconClass: 'fas fa-layer-group',
      order: 3,
      layout: eLayoutType.application,
    },
    {
      path: '/catalogues',
      name: '::Menu:Catalogue',
      parentName: '::Menu:Catalogues',
      iconClass: 'fas fa-layer-group',
      order: 1,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:Catalogue',
    },

    // ── Products (group) ──────────────────────────────────────────────────────
    {
      path: '',
      name: '::Menu:Products',
      iconClass: 'fas fa-box-open',
      order: 4,
      layout: eLayoutType.application,
    },
    {
      path: '/products',
      name: '::Menu:Product',
      parentName: '::Menu:Products',
      iconClass: 'fas fa-tag',
      order: 1,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:Product',
    },
    {
      path: '/stock-entries',
      name: '::Menu:StockEntry',
      parentName: '::Menu:Products',
      iconClass: 'fas fa-warehouse',
      order: 2,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:StockEntry',
    },
    {
      path: '/stock-ledger',
      name: '::Menu:StockLedger',
      parentName: '::Menu:Products',
      iconClass: 'fas fa-clipboard-list',
      order: 3,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:StockLedger',
    },

    // ── Sales (group) ─────────────────────────────────────────────────────────
    {
      path: '',
      name: '::Menu:Sales',
      iconClass: 'fas fa-chart-line',
      order: 5,
      layout: eLayoutType.application,
    },
    {
      path: '/point-of-sales',
      name: '::Menu:PointOfSales',
      parentName: '::Menu:Sales',
      iconClass: 'fas fa-cash-register',
      order: 1,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:PointOfSales',
    },
    {
      path: '/pos-counters',
      name: '::Menu:POSCounters',
      parentName: '::Menu:Sales',
      iconClass: 'fas fa-desktop',
      order: 2,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:POSCounters',
    },
    {
      path: '/sales-invoices',
      name: '::Menu:SalesInvoice',
      parentName: '::Menu:Sales',
      iconClass: 'fas fa-file-invoice-dollar',
      order: 3,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:SalesInvoice',
    },
    {
      path: '/product-returns',
      name: '::Menu:ProductReturn',
      parentName: '::Menu:Sales',
      iconClass: 'fas fa-undo-alt',
      order: 4,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:ProductReturn',
    },
    {
      path: '/promotions',
      name: '::Menu:Promotion',
      parentName: '::Menu:Sales',
      iconClass: 'fas fa-percentage',
      order: 5,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:Promotion',
    },
    {
      path: '/customers',
      name: '::Menu:Customer',
      parentName: '::Menu:Sales',
      iconClass: 'fas fa-users',
      order: 6,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:Customer',
    },

    // ── E-Commerce (group) ────────────────────────────────────────────────────
    {
      path: '',
      name: '::Menu:ECommerce',
      iconClass: 'fas fa-shopping-cart',
      order: 6,
      layout: eLayoutType.application,
    },
    {
      path: '/orders',
      name: '::Menu:Orders',
      parentName: '::Menu:ECommerce',
      iconClass: 'fas fa-shopping-bag',
      order: 1,
      layout: eLayoutType.application,
      breadcrumbText: '::Menu:Orders',
    },

  ]);
}
