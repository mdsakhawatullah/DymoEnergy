import { Component, OnDestroy, OnInit } from '@angular/core';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { CustomerService } from '../../proxy/customers/customer.service';
import {
  CUSTOMER_TYPES,
  CustomerListItemDto,
  CustomerOverviewDto,
  CustomerStatus,
  CustomersPageDto,
  CustomerType,
} from '../../proxy/customers/models';
import { fmtFull } from '../budgets-costs/finance.utils';
import { CustomerDrawerComponent } from './customer-drawer/customer-drawer.component';
import { CustomerEntryDrawerComponent } from './entry-drawer/customer-entry-drawer.component';

type TabKey = 'all' | 'households' | 'businesses' | 'owes' | 'quiet';

const PERM = { create: 'DymoEnergy.Customers.Create', edit: 'DymoEnergy.Customers.Edit', delete: 'DymoEnergy.Customers.Delete' };
const PAGE_SIZE = 12;

@Component({
  selector: 'app-customers',
  templateUrl: './customers.component.html',
  styleUrls: ['./customers.component.css'],
  imports: [SharedModule, CustomerDrawerComponent, CustomerEntryDrawerComponent],
})
export class CustomersComponent implements OnInit, OnDestroy {
  readonly Type = CustomerType;
  readonly Status = CustomerStatus;
  readonly types = CUSTOMER_TYPES;

  overview: CustomerOverviewDto | null = null;
  page: CustomersPageDto | null = null;
  loading = false;
  failed = false;

  tab: TabKey = 'all';
  filter = '';
  city: string | null = null;
  sorting = 'recent';
  pageIndex = 1;

  /** Id open in the detail drawer. */
  openId: number | null = null;
  /** Id being edited, or 0 for a new customer; null when the form is closed. */
  editId: number | null = null;

  importing = false;
  importDismissed = false;
  exporting = false;

  canCreate = false;
  canEdit = false;
  canDelete = false;

  readonly tabs: { key: TabKey; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'households', label: 'Households' },
    { key: 'businesses', label: 'Businesses & dealers' },
    { key: 'owes', label: 'Owes money' },
    { key: 'quiet', label: 'Gone quiet' },
  ];

  readonly sorts = [
    { value: 'recent', label: 'Bought most recently' },
    { value: 'spent', label: 'Spent the most' },
    { value: 'owed', label: 'Owes the most' },
    { value: 'orders', label: 'Most orders' },
    { value: 'name', label: 'Name A–Z' },
    { value: 'oldest', label: 'Longest since buying' },
  ];

  private search$ = new Subject<void>();
  private destroy$ = new Subject<void>();

  constructor(private api: CustomerService, private message: NzMessageService, permissions: PermissionService) {
    this.canCreate = permissions.getGrantedPolicy(PERM.create);
    this.canEdit = permissions.getGrantedPolicy(PERM.edit);
    this.canDelete = permissions.getGrantedPolicy(PERM.delete);
  }

  ngOnInit(): void {
    this.loadOverview();
    this.load();
    this.search$.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => { this.pageIndex = 1; this.load(); });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Loading ─────────────────────────────────────────────────────────────
  loadOverview(): void {
    this.api.getOverview().subscribe(o => (this.overview = o));
  }

  private query(skip: number, take: number) {
    return {
      filter: this.filter.trim() || undefined,
      type: this.tab === 'households' ? CustomerType.Household : undefined,
      city: this.city ?? undefined,
      owesMoney: this.tab === 'owes' || undefined,
      goneQuiet: this.tab === 'quiet' || undefined,
      sorting: this.sorting,
      skipCount: skip,
      maxResultCount: take,
    };
  }

  load(): void {
    this.loading = true;
    this.failed = false;
    this.api.getList(this.query((this.pageIndex - 1) * PAGE_SIZE, PAGE_SIZE)).subscribe({
      next: p => {
        // "Businesses & dealers" is everything that is not a household, which the API cannot express in one filter.
        this.page = this.tab === 'businesses'
          ? { ...p, items: p.items.filter(c => c.type !== CustomerType.Household) }
          : p;
        this.loading = false;
      },
      error: () => { this.loading = false; this.failed = true; },
    });
  }

  onSearch(): void {
    this.search$.next();
  }

  setTab(t: TabKey): void {
    this.tab = t;
    this.pageIndex = 1;
    this.load();
  }

  changed(): void {
    this.pageIndex = 1;
    this.load();
  }

  goTo(p: number): void {
    this.pageIndex = p;
    this.load();
  }

  get pageCount(): number {
    return Math.max(1, Math.ceil((this.page?.totalCount ?? 0) / PAGE_SIZE));
  }

  get pages(): (number | null)[] {
    const n = this.pageCount, c = this.pageIndex;
    if (n <= 7) return Array.from({ length: n }, (_, i) => i + 1);
    const set = new Set([1, 2, n, c - 1, c, c + 1].filter(p => p >= 1 && p <= n));
    if (c <= 3) [3, 4].forEach(p => set.add(p));
    const sorted = [...set].sort((a, b) => a - b);
    const out: (number | null)[] = [];
    sorted.forEach((p, i) => { if (i && p - sorted[i - 1] > 1) out.push(null); out.push(p); });
    return out;
  }

  count(key: TabKey): number {
    const c = this.page?.counts;
    if (!c) return 0;
    return { all: c.all, households: c.households, businesses: c.businesses, owes: c.owesMoney, quiet: c.goneQuiet }[key];
  }

  // ── Display ─────────────────────────────────────────────────────────────
  money = (v: number | null | undefined) => fmtFull(v ?? 0, '৳');

  initials(c: CustomerListItemDto): string {
    return (c.name || '?').split(' ').filter(Boolean).slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  typeLabel(t: CustomerType): string {
    return CUSTOMER_TYPES.find(x => x.value === t)?.label ?? 'Household';
  }

  lastOrderText(c: CustomerListItemDto): string {
    if (c.quietDays == null) return 'never ordered';
    if (c.quietDays === 0) return 'today';
    if (c.quietDays === 1) return 'yesterday';
    if (c.quietDays < 31) return `${c.quietDays} days ago`;
    const months = Math.round(c.quietDays / 30);
    return months < 12 ? `${months} ${months === 1 ? 'month' : 'months'} ago` : `${Math.round(c.quietDays / 365)}y ago`;
  }

  // ── Actions ─────────────────────────────────────────────────────────────
  create(): void {
    this.editId = 0;
  }

  edit(id: number): void {
    this.openId = null;
    this.editId = id;
  }

  saved(): void {
    this.editId = null;
    this.load();
    this.loadOverview();
  }

  refresh(): void {
    this.load();
    this.loadOverview();
  }

  /** Builds customers from orders that were taken before customers were kept. */
  importFromOrders(): void {
    this.importing = true;
    this.api.importFromOrders().subscribe({
      next: r => {
        this.importing = false;
        if (r.created > 0) this.message.success(r.message, { nzDuration: 8000 });
        else this.message.info(r.message, { nzDuration: 6000 });
        this.refresh();
      },
      error: () => { this.importing = false; this.message.error('Could not build the customers.'); },
    });
  }

  export(): void {
    this.exporting = true;
    this.api.getList(this.query(0, 1000)).subscribe({
      next: p => {
        this.exporting = false;
        const head = ['Name', 'Phone', 'Email', 'Type', 'Company', 'Where', 'Orders', 'Total spent', 'Owed', 'Last order', 'Status', 'Tags'];
        const rows = p.items.map(c => [
          c.name, c.phone ?? '', c.email ?? '', this.typeLabel(c.type), c.companyName ?? '', c.where ?? '',
          c.orders, c.totalSpent, c.owed, c.lastOrderAt ? new Date(c.lastOrderAt).toISOString().slice(0, 10) : '',
          CustomerStatus[c.status], c.tags.join('; '),
        ]);
        const csv = [head, ...rows].map(r => r.map(v => `"${String(v ?? '').replace(/"/g, '""')}"`).join(',')).join('\r\n');
        const url = URL.createObjectURL(new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' }));
        const a = document.createElement('a');
        a.href = url;
        a.download = `customers-${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => (this.exporting = false),
    });
  }
}
