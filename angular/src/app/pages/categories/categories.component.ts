import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { CategoryService } from '../../proxy/categories/category.service';
import { CategoryDto } from '../../proxy/categories/models';
import { CategoryEntryDrawerComponent } from './entry-drawer/category-entry-drawer.component';

type TabKey   = 'all' | 'published' | 'unpublished' | 'featured';
type StatsKey = 'total' | 'published' | 'unpublished' | 'featured';

interface CategoryStats {
  total:       number;
  published:   number;
  unpublished: number;
  featured:    number;
}

@Component({
  selector:    'app-categories',
  templateUrl: './categories.component.html',
  styleUrl:    './categories.component.css',
  imports:     [SharedModule, CategoryEntryDrawerComponent],
})
export class CategoriesComponent implements OnInit {

  // ── List state ────────────────────────────────────────────────────────────
  categories: CategoryDto[] = [];
  totalCount = 0;
  pageIndex  = 1;
  pageSize   = 10;
  filter     = '';
  loading    = false;
  activeTab: TabKey = 'all';

  stats: CategoryStats = { total: 0, published: 0, unpublished: 0, featured: 0 };

  tabs: { key: TabKey; label: string; countKey: StatsKey }[] = [
    { key: 'all',         label: 'All',         countKey: 'total'       },
    { key: 'published',   label: 'Published',   countKey: 'published'   },
    { key: 'unpublished', label: 'Unpublished', countKey: 'unpublished' },
    { key: 'featured',    label: 'Featured',    countKey: 'featured'    },
  ];

  // ── Drawer state ──────────────────────────────────────────────────────────
  isDrawerOpen                 = false;
  selectedCategory: CategoryDto | null = null;

  deletingIds = new Set<number>();

  constructor(
    private categoryService: CategoryService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadData();
  }

  // ── Drawer open / close ───────────────────────────────────────────────────
  openDrawerForCreate(): void {
    this.selectedCategory = null;
    this.isDrawerOpen      = true;
  }

  openDrawerForEdit(item: CategoryDto): void {
    this.selectedCategory = item;
    this.isDrawerOpen      = true;
  }

  onDrawerClosed(): void {
    this.isDrawerOpen = false;
  }

  onCategorySaved(): void {
    this.isDrawerOpen = false;
    this.loadStats();
    this.loadData();
  }

  // ── List ──────────────────────────────────────────────────────────────────
  loadStats(): void {
    forkJoin({
      all:       this.categoryService.getListData({ maxResultCount: 1, skipCount: 0 }),
      published: this.categoryService.getListData({ isPublished: true, maxResultCount: 1, skipCount: 0 }),
      featured:  this.categoryService.getListData({ isFeatured:  true, maxResultCount: 1, skipCount: 0 }),
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

    this.categoryService.getListData(params).subscribe({
      next: result => {
        this.categories = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
      },
      error: () => { this.loading = false; },
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────────
  deleteCategory(id: number): void {
    this.deletingIds.add(id);
    this.categoryService.delete(id).subscribe({
      next: () => {
        this.message.success('Category deleted.');
        this.deletingIds.delete(id);
        this.loadStats();
        this.loadData();
      },
      error: () => {
        this.message.error('Failed to delete category.');
        this.deletingIds.delete(id);
      },
    });
  }

  selectTab(tab: TabKey): void { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearch(): void              { this.pageIndex = 1; this.loadData(); }
  resetFilters(): void          { this.filter = ''; this.activeTab = 'all'; this.pageIndex = 1; this.loadData(); }
  onPageIndexChange(i: number)  { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)   { this.pageSize = s; this.pageIndex = 1; this.loadData(); }
}
