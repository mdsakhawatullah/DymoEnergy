import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { ProductService } from '../../proxy/products/product.service';
import { ProductDto, ProductFilterDto, ProductStatusColors, ProductStatusLabels } from '../../proxy/products/models';
import { CategoryService } from '../../proxy/categories/category.service';
import { SelectListDto } from '../../proxy/categories/models';
import { ProductEntryDrawerComponent } from './entry-drawer/product-entry-drawer.component';

type TabKey = 'all' | 'active' | 'draft' | 'featured' | 'attention';

const EXPORT_LIMIT = 1000;

interface ProductStats {
  all:       number;
  active:    number;
  draft:     number;
  featured:  number;
  attention: number;
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

  stats: ProductStats = { all: 0, active: 0, draft: 0, featured: 0, attention: 0 };
  categoryCount = 0;
  exporting = false;

  tabs: { key: TabKey; label: string }[] = [
    { key: 'all',       label: 'All' },
    { key: 'active',    label: 'Active' },
    { key: 'draft',     label: 'Draft' },
    { key: 'featured',  label: 'Featured' },
    { key: 'attention', label: 'Needs attention' },
  ];

  // ── Filters ───────────────────────────────────────────────────────────────
  categoryId: number | null = null;
  stockState: number | null = null;
  sorting = 'newest';

  categories: SelectListDto[] = [];

  stockOptions = [
    { value: 1, label: 'In stock' },
    { value: 2, label: 'Low stock' },
    { value: 3, label: 'Out of stock' },
  ];

  sortOptions = [
    { value: 'newest',     label: 'Newest first' },
    { value: 'oldest',     label: 'Oldest first' },
    { value: 'name',       label: 'Name A–Z' },
    { value: 'price-asc',  label: 'Price: low to high' },
    { value: 'price-desc', label: 'Price: high to low' },
    { value: 'stock-asc',  label: 'Lowest stock' },
  ];

  // ── Drawer state ──────────────────────────────────────────────────────────
  isDrawerOpen                  = false;
  selectedProduct: ProductDto | null = null;

  deletingIds = new Set<number>();

  statusLabels = ProductStatusLabels;
  statusColors = ProductStatusColors;

  constructor(
    private productService:  ProductService,
    private categoryService: CategoryService,
    private message:         NzMessageService,
  ) {}

  ngOnInit(): void {
    this.categoryService.getSelectList().subscribe(items => this.categories = items);
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
      featured:  this.productService.getListData({ isFeatured: true, maxResultCount: 1, skipCount: 0 }),
      attention: this.productService.getListData({ needsAttention: true, maxResultCount: 1, skipCount: 0 }),
      categories: this.categoryService.getListData({ maxResultCount: 1, skipCount: 0 }),
    }).subscribe(r => {
      this.stats = {
        all:       r.all.totalCount,
        active:    r.active.totalCount,
        draft:     r.draft.totalCount,
        featured:  r.featured.totalCount,
        attention: r.attention.totalCount,
      };
      this.categoryCount = r.categories.totalCount;
    });
  }

  /** Filters shared by the table and the CSV export, so the file matches what's on screen. */
  private filterParams(): ProductFilterDto {
    const params: ProductFilterDto = {
      filter:     this.filter.trim() || undefined,
      categoryId: this.categoryId ?? undefined,
      stockState: this.stockState ?? undefined,
      sorting:    this.sorting,
    };
    if (this.activeTab === 'active')    params.status         = 2;
    if (this.activeTab === 'draft')     params.status         = 1;
    if (this.activeTab === 'featured')  params.isFeatured     = true;
    if (this.activeTab === 'attention') params.needsAttention = true;
    return params;
  }

  get hasFilters(): boolean {
    return !!this.filter.trim() || this.activeTab !== 'all'
        || this.categoryId !== null || this.stockState !== null || this.sorting !== 'newest';
  }

  loadData(): void {
    this.loading = true;
    const params: ProductFilterDto = {
      ...this.filterParams(),
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };

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

  // ── Export ────────────────────────────────────────────────────────────────
  exportCsv(): void {
    this.exporting = true;
    this.productService.getListData({ ...this.filterParams(), skipCount: 0, maxResultCount: EXPORT_LIMIT }).subscribe({
      next: result => {
        const header = ['ID', 'Name', 'SKU', 'Slug', 'Status', 'Featured', 'Price', 'Discount price', 'Stock', 'Weight'];
        const rows = result.items.map(p => [
          p.id, p.name, p.sku, p.slug, this.statusLabels[p.status], p.isFeatured ? 'Yes' : 'No',
          p.price, p.discountPrice ?? '', p.stockQuantity, p.weight ?? '',
        ]);

        // Quote every cell so commas and quotes inside names survive
        const cell = (v: unknown) => `"${String(v ?? '').replace(/"/g, '""')}"`;
        const csv  = [header, ...rows].map(r => r.map(cell).join(',')).join('\r\n');

        // BOM so Excel opens Bangla text and the ৳ sign correctly
        const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' });
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `products-${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        URL.revokeObjectURL(a.href);

        if (result.totalCount > EXPORT_LIMIT) {
          this.message.warning(`Exported the first ${EXPORT_LIMIT} of ${result.totalCount} products.`);
        }
        this.exporting = false;
      },
      error: () => {
        this.message.error('Export failed.');
        this.exporting = false;
      },
    });
  }

  selectTab(tab: TabKey): void  { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearch(): void               { this.pageIndex = 1; this.loadData(); }
  onFilterChange(): void         { this.pageIndex = 1; this.loadData(); }

  resetFilters(): void {
    this.filter     = '';
    this.activeTab  = 'all';
    this.categoryId = null;
    this.stockState = null;
    this.sorting    = 'newest';
    this.pageIndex  = 1;
    this.loadData();
  }
  onPageIndexChange(i: number)   { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)    { this.pageSize = s; this.pageIndex = 1; this.loadData(); }
}
