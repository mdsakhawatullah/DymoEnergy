import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { CatalogueService } from '../../proxy/catalogues/catalogue.service';
import { CatalogueDto } from '../../proxy/catalogues/models';
import { CatalogueEntryDrawerComponent } from './catalogue-entry-drawer.component';

type TabKey   = 'all' | 'published' | 'unpublished' | 'featured';
type StatsKey = 'total' | 'published' | 'unpublished' | 'featured';

interface CatalogueStats {
  total:       number;
  published:   number;
  unpublished: number;
  featured:    number;
}

@Component({
  selector:    'app-catalogues',
  templateUrl: './catalogues.component.html',
  styleUrl:    './catalogues.component.css',
  imports:     [SharedModule, CatalogueEntryDrawerComponent],
})
export class CataloguesComponent implements OnInit {

  // ── List state ────────────────────────────────────────────────────────────
  catalogues: CatalogueDto[] = [];
  totalCount = 0;
  pageIndex  = 1;
  pageSize   = 10;
  filter     = '';
  loading    = false;
  activeTab: TabKey = 'all';

  stats: CatalogueStats = { total: 0, published: 0, unpublished: 0, featured: 0 };

  tabs: { key: TabKey; label: string; countKey: StatsKey }[] = [
    { key: 'all',         label: 'All',         countKey: 'total'       },
    { key: 'published',   label: 'Published',   countKey: 'published'   },
    { key: 'unpublished', label: 'Unpublished', countKey: 'unpublished' },
    { key: 'featured',    label: 'Featured',    countKey: 'featured'    },
  ];

  // ── Drawer state ──────────────────────────────────────────────────────────
  isDrawerOpen                 = false;
  selectedCatalogue: CatalogueDto | null = null;

  constructor(private catalogueService: CatalogueService) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadData();
  }

  // ── Drawer open / close ───────────────────────────────────────────────────
  openDrawerForCreate(): void {
    this.selectedCatalogue = null;
    this.isDrawerOpen      = true;
  }

  openDrawerForEdit(item: CatalogueDto): void {
    this.selectedCatalogue = item;
    this.isDrawerOpen      = true;
  }

  onDrawerClosed(): void {
    this.isDrawerOpen = false;
  }

  onCatalogueSaved(): void {
    this.isDrawerOpen = false;
    this.loadStats();
    this.loadData();
  }

  // ── List ──────────────────────────────────────────────────────────────────
  loadStats(): void {
    forkJoin({
      all:       this.catalogueService.getListData({ maxResultCount: 1, skipCount: 0 }),
      published: this.catalogueService.getListData({ isPublished: true, maxResultCount: 1, skipCount: 0 }),
      featured:  this.catalogueService.getListData({ isFeatured:  true, maxResultCount: 1, skipCount: 0 }),
    }).subscribe(results => {
      this.stats.total       = results.all.totalCount;
      this.stats.published   = results.published.totalCount;
      this.stats.featured    = results.featured.totalCount;
      this.stats.unpublished = results.all.totalCount - results.published.totalCount;
    });
  }

  loadData(): void {
    this.loading = true;
    const params: { filter?: string; skipCount: number; maxResultCount: number; isPublished?: boolean; isFeatured?: boolean } = {
      filter:         this.filter || undefined,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };
    if (this.activeTab === 'published')   params.isPublished = true;
    if (this.activeTab === 'unpublished') params.isPublished = false;
    if (this.activeTab === 'featured')    params.isFeatured  = true;

    this.catalogueService.getListData(params).subscribe({
      next: result => {
        this.catalogues = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
      },
      error: () => { this.loading = false; },
    });
  }

  selectTab(tab: TabKey): void { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearch(): void              { this.pageIndex = 1; this.loadData(); }
  resetFilters(): void          { this.filter = ''; this.activeTab = 'all'; this.pageIndex = 1; this.loadData(); }
  onPageIndexChange(i: number)  { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)   { this.pageSize = s; this.pageIndex = 1; this.loadData(); }
}
