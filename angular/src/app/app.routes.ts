import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';

const comingSoonComponent = () => import('./pages/coming-soon/coming-soon.component').then(c => c.ComingSoonComponent);

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

  // ── Site Settings ────────────────────────────────────────────────────────────
  {
    path: 'admin-site-settings',
    loadComponent: () => import('./pages/admin-site-settings/admin-site-settings.component').then(c => c.AdminSiteSettingsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'user-site-settings',
    loadComponent: () => import('./pages/user-site-settings/user-site-settings.component').then(c => c.UserSiteSettingsComponent),
    canActivate: [authGuard],
  },

  // ── Project Planning & Execution ──────────────────────────────────────────
  {
    path: 'projects',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'phases-timelines',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'milestones-goals',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'team-assignment',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'resource-allocation',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Asset & Site Management ───────────────────────────────────────────────
  {
    path: 'sites',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'assets',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'preventive-maintenance',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'asset-performance',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Site Inspections & Subcontracts ───────────────────────────────────────
  {
    path: 'inspections',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'inspection-teams',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'subcontracts',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Inventory & Procurement ───────────────────────────────────────────────
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
  {
    path: 'suppliers',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'purchase-orders',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'quotes-deliveries',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Workforce & Task Management ───────────────────────────────────────────
  {
    path: 'technicians',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'tasks',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'attendance',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'shift-scheduling',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'payroll',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Maintenance & Service Management ──────────────────────────────────────
  {
    path: 'work-orders',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'maintenance-plans',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'repair-history',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'service-costs',
    loadComponent: comingSoonComponent,
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
  {
    path: 'catalogues',
    loadComponent: () => import('./pages/catalogues/catalogues.component').then(c => c.CataloguesComponent),
    canActivate: [authGuard],
  },

  // ── Companies ──────────────────────────────────────────────────────────────
  {
    path: 'companies',
    loadComponent: () => import('./pages/companies/companies.component').then(c => c.CompaniesComponent),
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
  {
    path: 'quote-requests',
    loadComponent: () => import('./pages/quote-requests/quote-requests.component').then(c => c.QuoteRequestsComponent),
    canActivate: [authGuard],
  },

  // ── Shipping & Courier Setup ───────────────────────────────────────────────
  {
    path: 'shipping-setup',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Financials & Billing ───────────────────────────────────────────────────
  {
    path: 'budgets-costs',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'service-invoices',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'funding-sources',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Compliance & Documentation ────────────────────────────────────────────
  {
    path: 'audits-inspections',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'compliance-documents',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'safety-regulations',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Analytics & Reporting ────────────────────────────────────────────────
  {
    path: 'dashboard',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'reports',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'charts-views',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },

  // ── Marketing ────────────────────────────────────────────────────────────
  {
    path: 'marketing-overview',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'integrations',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'sms-campaigns',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
  {
    path: 'email-campaigns',
    loadComponent: comingSoonComponent,
    canActivate: [authGuard],
  },
];
