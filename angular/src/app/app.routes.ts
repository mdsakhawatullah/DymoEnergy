import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';

export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },

  // ── ABP default modules ────────────────────────────────────────────────────
  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(c => c.createRoutes()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },
  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },

  // ── Books (existing) ──────────────────────────────────────────────────────
  {
    path: 'books',
    loadComponent: () => import('./book/book.component').then(c => c.BookComponent),
    canActivate: [authGuard, permissionGuard],
  },

  // ── Admin Site Settings ────────────────────────────────────────────────────
  {
    path: 'admin-site-settings',
    loadComponent: () => import('./pages/admin-site-settings/admin-site-settings.component').then(c => c.AdminSiteSettingsComponent),
    canActivate: [authGuard],
  },

  // ── User Site Settings ────────────────────────────────────────────────────
  {
    path: 'user-site-settings',
    loadComponent: () => import('./pages/user-site-settings/user-site-settings.component').then(c => c.UserSiteSettingsComponent),
    canActivate: [authGuard],
  },

  // ── Catalogues ─────────────────────────────────────────────────────────────
  {
    path: 'catalogues',
    loadComponent: () => import('./pages/catalogues/catalogues.component').then(c => c.CataloguesComponent),
    canActivate: [authGuard],
  },

  // ── Products ───────────────────────────────────────────────────────────────
  {
    path: 'products',
    loadComponent: () => import('./pages/products/products.component').then(c => c.ProductsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'stock-entries',
    loadComponent: () => import('./pages/stock-entries/stock-entries.component').then(c => c.StockEntriesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'stock-ledger',
    loadComponent: () => import('./pages/stock-ledger/stock-ledger.component').then(c => c.StockLedgerComponent),
    canActivate: [authGuard],
  },

  // ── Sales ──────────────────────────────────────────────────────────────────
  {
    path: 'point-of-sales',
    loadComponent: () => import('./pages/point-of-sales/point-of-sales.component').then(c => c.PointOfSalesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'pos-counters',
    loadComponent: () => import('./pages/pos-counters/pos-counters.component').then(c => c.PosCountersComponent),
    canActivate: [authGuard],
  },
  {
    path: 'sales-invoices',
    loadComponent: () => import('./pages/sales-invoices/sales-invoices.component').then(c => c.SalesInvoicesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'sales-invoices/:id',
    loadComponent: () => import('./pages/sales-invoices/invoice-details/invoice-details.component').then(c => c.InvoiceDetailsComponent),
    // No authGuard: reads are [AllowAnonymous] on backend; guard can redirect in new-tab context
  },
  {
    path: 'product-returns',
    loadComponent: () => import('./pages/product-returns/product-returns.component').then(c => c.ProductReturnsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'promotions',
    loadComponent: () => import('./pages/promotions/promotions.component').then(c => c.PromotionsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'customers',
    loadComponent: () => import('./pages/customers/customers.component').then(c => c.CustomersComponent),
    canActivate: [authGuard],
  },

  // ── E-Commerce ─────────────────────────────────────────────────────────────
  // IMPORTANT: specific route must come before the generic 'orders' prefix route
  {
    path: 'orders/:id/invoice',
    loadComponent: () => import('./pages/orders/invoice/order-invoice.component').then(c => c.OrderInvoiceComponent),
    // No authGuard: GET endpoints are [AllowAnonymous]; guard can redirect in new-tab context
  },
  {
    path: 'orders',
    loadComponent: () => import('./pages/orders/orders.component').then(c => c.OrdersComponent),
    canActivate: [authGuard],
  },
];
