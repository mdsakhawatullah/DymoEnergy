import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { ProductService } from '../../proxy/products/product.service';
import { ProductDto, ProductStatusColors, ProductStatusLabels } from '../../proxy/products/models';
import { ProductEntryDrawerComponent } from './entry-drawer/product-entry-drawer.component';

type TabKey   = 'all' | 'active' | 'draft' | 'featured';
type StatsKey = 'total' | 'active' | 'draft' | 'featured';

interface ProductStats {
  total:    number;
  active:   number;
  draft:    number;
  featured: number;
}

@Component({
  selector:    'app-products',
  templateUrl: './products.component.html',
  styleUrl:    './products.component.css',
  imports:     [SharedModule, ProductEntryDrawerComponent]
})
export class ProductsComponent implements OnInit {

  // ── List state ────────────────────────────────────────────────────────────
  products:   ProductDto[] = [];
  totalCount  = 0;
  pageIndex   = 1;
  pageSize    = 10;
  filter      = '';
  loading     = false;
  activeTab: TabKey = 'all';

  stats: ProductStats = { total: 0, active: 0, draft: 0, featured: 0 };

  tabs: { key: TabKey; label: string; countKey: StatsKey }[] = [
    { key: 'all',      label: 'All',      countKey: 'total'    },
    { key: 'active',   label: 'Active',   countKey: 'active'   },
    { key: 'draft',    label: 'Draft',    countKey: 'draft'    },
    { key: 'featured', label: 'Featured', countKey: 'featured' },
  ];

  // ── Drawer state ──────────────────────────────────────────────────────────
  isDrawerOpen                  = false;
  selectedProduct: ProductDto | null = null;

  deletingIds = new Set<number>();

  statusLabels = ProductStatusLabels;
  statusColors = ProductStatusColors;

  constructor(
    private productService: ProductService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadData();
  }

  // ── Drawer open / close ───────────────────────────────────────────────────
  openDrawerForCreate(): void {
    this.selectedProduct = null;
    this.isDrawerOpen    = true;
  }

  openDrawerForEdit(item: ProductDto): void {
    this.selectedProduct = item;
    this.isDrawerOpen    = true;
  }

  onDrawerClosed(): void  { this.isDrawerOpen = false; }

  onProductSaved(): void {
    this.isDrawerOpen = false;
    this.loadStats();
    this.loadData();
  }

  // ── List ──────────────────────────────────────────────────────────────────
  loadStats(): void {
    forkJoin({
      all:      this.productService.getListData({ maxResultCount: 1, skipCount: 0 }),
      active:   this.productService.getListData({ status: 2, maxResultCount: 1, skipCount: 0 }),
      draft:    this.productService.getListData({ status: 1, maxResultCount: 1, skipCount: 0 }),
      featured: this.productService.getListData({ isFeatured: true, maxResultCount: 1, skipCount: 0 }),
    }).subscribe(r => {
      this.stats.total    = r.all.totalCount;
      this.stats.active   = r.active.totalCount;
      this.stats.draft    = r.draft.totalCount;
      this.stats.featured = r.featured.totalCount;
    });
  }

  loadData(): void {
    this.loading = true;
    const params: any = {
      filter:         this.filter || undefined,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };
    if (this.activeTab === 'active')   params.status    = 2;
    if (this.activeTab === 'draft')    params.status    = 1;
    if (this.activeTab === 'featured') params.isFeatured = true;

    this.productService.getListData(params).subscribe({
      next: result => {
        this.products   = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
      },
      error: () => { this.loading = false; },
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────────
  deleteProduct(id: number): void {
    this.deletingIds.add(id);
    this.productService.delete(id).subscribe({
      next: () => {
        this.message.success('Product deleted.');
        this.deletingIds.delete(id);
        this.loadStats();
        this.loadData();
      },
      error: () => {
        this.message.error('Failed to delete product.');
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
