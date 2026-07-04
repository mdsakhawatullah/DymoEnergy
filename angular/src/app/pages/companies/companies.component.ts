import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { CompanyService } from '../../proxy/companies/company.service';
import { CompanyDto, CompanyStatusColors, CompanyStatusLabels } from '../../proxy/companies/models';
import { CompanyEntryDrawerComponent } from './entry-drawer/company-entry-drawer.component';

type TabKey   = 'all' | 'active' | 'parent' | 'child';
type StatsKey = 'total' | 'active' | 'parent' | 'child';

interface CompanyStats {
  total:  number;
  active: number;
  parent: number;
  child:  number;
}

@Component({
  selector:    'app-companies',
  templateUrl: './companies.component.html',
  styleUrl:    './companies.component.css',
  imports:     [SharedModule, CompanyEntryDrawerComponent]
})
export class CompaniesComponent implements OnInit {

  // ── List state ────────────────────────────────────────────────────────────
  companies:  CompanyDto[] = [];
  totalCount  = 0;
  pageIndex   = 1;
  pageSize    = 10;
  filter      = '';
  loading     = false;
  activeTab: TabKey = 'all';

  stats: CompanyStats = { total: 0, active: 0, parent: 0, child: 0 };

  tabs: { key: TabKey; label: string; countKey: StatsKey }[] = [
    { key: 'all',    label: 'All',              countKey: 'total'  },
    { key: 'active', label: 'Active',           countKey: 'active' },
    { key: 'parent', label: 'Parent Companies', countKey: 'parent' },
    { key: 'child',  label: 'Child Companies',  countKey: 'child'  },
  ];

  // ── Drawer state ──────────────────────────────────────────────────────────
  isDrawerOpen                 = false;
  selectedCompany: CompanyDto | null = null;

  deletingIds = new Set<number>();

  statusLabels = CompanyStatusLabels;
  statusColors = CompanyStatusColors;

  constructor(
    private companyService: CompanyService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadData();
  }

  // ── Drawer open / close ───────────────────────────────────────────────────
  openDrawerForCreate(): void {
    this.selectedCompany = null;
    this.isDrawerOpen    = true;
  }

  openDrawerForEdit(item: CompanyDto): void {
    this.selectedCompany = item;
    this.isDrawerOpen    = true;
  }

  onDrawerClosed(): void { this.isDrawerOpen = false; }

  onCompanySaved(): void {
    this.isDrawerOpen = false;
    this.loadStats();
    this.loadData();
  }

  // ── List ──────────────────────────────────────────────────────────────────
  loadStats(): void {
    forkJoin({
      all:    this.companyService.getListData({ maxResultCount: 1, skipCount: 0 }),
      active: this.companyService.getListData({ status: 1, maxResultCount: 1, skipCount: 0 }),
      parent: this.companyService.getListData({ isParentCompany: true, maxResultCount: 1, skipCount: 0 }),
      child:  this.companyService.getListData({ isParentCompany: false, maxResultCount: 1, skipCount: 0 }),
    }).subscribe(r => {
      this.stats.total  = r.all.totalCount;
      this.stats.active = r.active.totalCount;
      this.stats.parent = r.parent.totalCount;
      this.stats.child  = r.child.totalCount;
    });
  }

  loadData(): void {
    this.loading = true;
    const params: any = {
      filter:         this.filter || undefined,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };
    if (this.activeTab === 'active') params.status          = 1;
    if (this.activeTab === 'parent') params.isParentCompany  = true;
    if (this.activeTab === 'child')  params.isParentCompany  = false;

    this.companyService.getListData(params).subscribe({
      next: result => {
        this.companies  = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
      },
      error: () => { this.loading = false; },
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────────
  deleteCompany(id: number): void {
    this.deletingIds.add(id);
    this.companyService.delete(id).subscribe({
      next: () => {
        this.message.success('Company deleted.');
        this.deletingIds.delete(id);
        this.loadStats();
        this.loadData();
      },
      error: () => {
        this.message.error('Failed to delete company.');
        this.deletingIds.delete(id);
      },
    });
  }

  selectTab(tab: TabKey): void  { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearch(): void               { this.pageIndex = 1; this.loadData(); }
  resetFilters(): void           { this.filter = ''; this.activeTab = 'all'; this.pageIndex = 1; this.loadData(); }
  onPageIndexChange(i: number)   { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)    { this.pageSize = s; this.pageIndex = 1; this.loadData(); }
}
