import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ProductService } from '../../../proxy/products/product.service';
import { ProductDto, ProductStatusLabels } from '../../../proxy/products/models';
import { CategoryService } from '../../../proxy/categories/category.service';
import { SelectListDto } from '../../../proxy/categories/models';
import { ImageUploadService } from '../../../proxy/image-upload/image-upload.service';

type SectionKey = 'basic' | 'pricing' | 'photos' | 'visibility';

/** A photo slot: `id` is set for gallery images that already exist on the server. */
interface Photo {
  id?: number;
  url: string;
}

const MAX_PHOTOS    = 8;
const MAX_BYTES     = 2 * 1024 * 1024;
const LOW_STOCK     = 10;
const SUMMARY_MAX   = 160;
const ACCEPTED      = ['image/jpeg', 'image/png', 'image/webp'];

@Component({
  selector:    'product-entry-drawer',
  templateUrl: './product-entry-drawer.component.html',
  styleUrl:    './product-entry-drawer.component.css',
  imports:     [SharedModule],
})
export class ProductEntryDrawerComponent implements OnChanges, OnInit {

  @Input()  input: ProductDto | null = null;

  @Output() onProductEntryDrawerClosed = new EventEmitter<void>();
  @Output() handleProductSaved         = new EventEmitter<void>();

  form!: FormGroup;
  saving     = false;
  loading    = false;
  showErrors = false;
  showMore   = false;

  categoryOptions: SelectListDto[] = [];

  photos: Photo[] = [];
  uploading = 0;
  dragIndex: number | null = null;
  dropActive = false;

  /** Slug follows the name until the user chooses to edit it by hand. */
  slugEditing = false;

  readonly maxPhotos  = MAX_PHOTOS;
  readonly summaryMax = SUMMARY_MAX;
  readonly lowStock   = LOW_STOCK;

  sections: { key: SectionKey; label: string }[] = [
    { key: 'basic',      label: 'Basic info' },
    { key: 'pricing',    label: 'Pricing & inventory' },
    { key: 'photos',     label: 'Photos' },
    { key: 'visibility', label: 'Visibility' },
  ];

  constructor(
    private fb:             FormBuilder,
    private productSvc:     ProductService,
    private categorySvc:    CategoryService,
    private message:        NzMessageService,
    private imageUploadSvc: ImageUploadService,
  ) {
    this.buildForm();
  }

  ngOnInit(): void {
    this.categorySvc.getSelectList().subscribe({
      next: items => this.categoryOptions = items,
      error: () => this.message.warning('Could not load categories.'),
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['input']) return;

    this.showErrors  = false;
    this.slugEditing = false;

    if (!this.input?.id) {
      this.form.reset({
        status: 2, isActive: true, isFeatured: false, displayOrder: 0,
        stockQuantity: 0, price: null, categoryId: null,
      });
      this.photos = [];
      return;
    }

    // The list endpoint omits gallery images, and saving sends the full image set
    // back (anything missing is deleted server-side) — so load the complete record.
    this.loading = true;
    this.productSvc.get(this.input.id).subscribe({
      next: p => {
        this.patch(p);
        this.loading = false;
      },
      error: () => {
        this.patch(this.input!);
        this.loading = false;
        this.message.warning('Could not load all photos for this product.');
      },
    });
  }

  private patch(p: ProductDto): void {
    this.form.patchValue({
      categoryId:      p.categoryId,
      name:            p.name,
      slug:            p.slug,
      summary:         p.summary,
      sku:             p.sku,
      price:           p.price,
      discountPrice:   p.discountPrice,
      weight:          this.parseWeight(p.weight),
      stockQuantity:   p.stockQuantity,
      description:     p.description,
      status:          p.status,
      isActive:        p.isActive,
      isFeatured:      p.isFeatured,
      displayOrder:    p.displayOrder,
      metaTitle:       p.metaTitle,
      metaDescription: p.metaDescription,
      metaKeywords:    p.metaKeywords,
    });

    // Main photo first, then the gallery in its saved order
    const gallery = [...(p.images ?? [])]
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .filter(i => !!i.imageUrl && i.imageUrl !== p.primaryImage)
      .map(i => ({ id: i.id, url: i.imageUrl! }));

    this.photos = [
      ...(p.primaryImage ? [{ url: p.primaryImage }] : []),
      ...gallery,
    ].slice(0, MAX_PHOTOS);

    // An existing slug that no longer matches the name was set by hand
    this.slugEditing = !!p.slug && p.slug !== this.slugify(p.name ?? '');
  }

  buildForm(): void {
    this.form = this.fb.group({
      categoryId:      [null, Validators.required],
      name:            [null, [Validators.required, Validators.maxLength(200)]],
      slug:            [null],
      summary:         [null, Validators.maxLength(SUMMARY_MAX)],
      sku:             [null],
      price:           [null, [Validators.required, Validators.min(1)]],
      discountPrice:   [null, Validators.min(0)],
      weight:          [null, Validators.min(0)],
      stockQuantity:   [0, Validators.min(0)],
      description:     [null],
      status:          [2],
      isActive:        [true],
      isFeatured:      [false],
      displayOrder:    [0],
      metaTitle:       [null],
      metaDescription: [null],
      metaKeywords:    [null],
    });

    this.form.get('name')!.valueChanges.subscribe((name: string) => {
      if (!this.slugEditing) {
        this.form.get('slug')!.setValue(this.slugify(name ?? ''), { emitEvent: false });
      }
    });
  }

  // ── Header & progress ──────────────────────────────────────────────────────
  get isEdit(): boolean { return !!this.input?.id; }

  get statusLabel(): string {
    return ProductStatusLabels[this.form.value.status] ?? 'Draft';
  }

  get isLive(): boolean { return this.form.value.status === 2; }

  sectionDone(key: SectionKey): boolean {
    const v = this.form.value;
    switch (key) {
      case 'basic':      return !!v.name?.trim() && !!v.categoryId;
      case 'pricing':    return (v.price ?? 0) > 0 && !this.discountInvalid;
      case 'photos':     return this.photos.length > 0;
      case 'visibility': return true;
    }
  }

  get doneCount(): number {
    return this.sections.filter(s => this.sectionDone(s.key)).length;
  }

  scrollTo(key: SectionKey): void {
    document.getElementById('pe-' + key)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  invalid(name: string): boolean {
    const c = this.form.get(name);
    return !!c && c.invalid && (c.touched || this.showErrors);
  }

  // ── Basic info ─────────────────────────────────────────────────────────────
  editSlug(): void {
    this.slugEditing = true;
    setTimeout(() => document.getElementById('pe-slug')?.focus());
  }

  onSlugBlur(): void {
    const slug = this.slugify(this.form.value.slug ?? '');
    this.form.get('slug')!.setValue(slug);
    // Cleared by hand → go back to following the name
    if (!slug) {
      this.slugEditing = false;
      this.form.get('slug')!.setValue(this.slugify(this.form.value.name ?? ''));
    }
  }

  generateSku(): void {
    const cat = this.categoryOptions.find(c => c.value === this.form.value.categoryId)?.displayText ?? '';
    const prefix = cat.split(/\s+/).filter(Boolean).map(w => w[0]).join('').toUpperCase().slice(0, 3) || 'DE';

    // The rating ("550W", "5kWh", "100Ah") is what staff search SKUs by
    const rating = (this.form.value.name ?? '').match(/\d+(\.\d+)?\s*(kwh|kw|wp|w|ah|v|hp)\b/i)?.[0]
      .replace(/\s+/g, '').toUpperCase();

    const serial = String(Math.floor(Math.random() * 1000)).padStart(3, '0');
    this.form.get('sku')!.setValue([prefix, rating, serial].filter(Boolean).join('-'));
  }

  get summaryLength(): number { return (this.form.value.summary ?? '').length; }

  // ── Pricing ────────────────────────────────────────────────────────────────
  get price(): number    { return +(this.form.value.price ?? 0); }
  get discount(): number { return +(this.form.value.discountPrice ?? 0); }

  get hasDiscount(): boolean { return this.discount > 0 && this.discount < this.price; }

  get discountInvalid(): boolean { return this.discount > 0 && this.discount >= this.price; }

  get sellPrice(): number { return this.hasDiscount ? this.discount : this.price; }

  get savePercent(): number {
    return this.hasDiscount ? Math.round((1 - this.discount / this.price) * 100) : 0;
  }

  stepStock(delta: number): void {
    const next = Math.max(0, (+this.form.value.stockQuantity || 0) + delta);
    this.form.get('stockQuantity')!.setValue(next);
  }

  get stockNote(): string {
    const q = +this.form.value.stockQuantity || 0;
    if (q === 0) return 'Out of stock — customers can still see it but can\'t order.';
    if (q <= LOW_STOCK) return `Low stock. "Low stock" shows at ${LOW_STOCK} or fewer.`;
    return `In stock. "Low stock" shows at ${LOW_STOCK} or fewer.`;
  }

  get stockTone(): 'ok' | 'low' | 'out' {
    const q = +this.form.value.stockQuantity || 0;
    return q === 0 ? 'out' : q <= LOW_STOCK ? 'low' : 'ok';
  }

  money(n: number): string { return Math.round(n || 0).toLocaleString('en-IN'); }

  // ── Photos ─────────────────────────────────────────────────────────────────
  get slotsLeft(): number { return MAX_PHOTOS - this.photos.length - this.uploading; }

  /** One placeholder tile per in-flight upload. */
  get uploadSlots(): number[] { return Array.from({ length: this.uploading }, (_, i) => i); }

  pickFiles(): void {
    document.getElementById('pe-file')?.click();
  }

  onFilesPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.addFiles(Array.from(input.files ?? []));
    input.value = '';  // allow picking the same file again
  }

  onDropZoneOver(e: DragEvent): void {
    if (e.dataTransfer?.types.includes('Files')) {
      e.preventDefault();
      this.dropActive = true;
    }
  }

  onDropZoneDrop(e: DragEvent): void {
    e.preventDefault();
    this.dropActive = false;
    this.addFiles(Array.from(e.dataTransfer?.files ?? []));
  }

  private addFiles(files: File[]): void {
    const room = this.slotsLeft;
    if (room <= 0) {
      this.message.warning(`A product can have up to ${MAX_PHOTOS} photos.`);
      return;
    }

    const valid = files.filter(f => {
      if (!ACCEPTED.includes(f.type)) {
        this.message.error(`${f.name}: use JPG, PNG or WebP.`);
        return false;
      }
      if (f.size > MAX_BYTES) {
        this.message.error(`${f.name} is over 2 MB.`);
        return false;
      }
      return true;
    });

    if (valid.length > room) {
      this.message.warning(`Only ${room} more photo${room === 1 ? '' : 's'} fit — the rest were skipped.`);
    }

    valid.slice(0, room).forEach(file => {
      this.uploading++;
      this.imageUploadSvc.uploadImage(file, 'products').subscribe({
        next: url => {
          this.photos = [...this.photos, { url }];
          this.uploading--;
        },
        error: () => {
          this.uploading--;
          this.message.error(`Could not upload ${file.name}.`);
        },
      });
    });
  }

  removePhoto(i: number): void {
    this.photos = this.photos.filter((_, idx) => idx !== i);
  }

  makeMain(i: number): void {
    if (i === 0) return;
    const next = [...this.photos];
    const [p] = next.splice(i, 1);
    this.photos = [p, ...next];
  }

  onTileDragStart(i: number): void { this.dragIndex = i; }

  onTileDragOver(e: DragEvent): void {
    if (this.dragIndex !== null) e.preventDefault();
  }

  onTileDrop(target: number): void {
    const from = this.dragIndex;
    this.dragIndex = null;
    if (from === null || from === target) return;

    const next = [...this.photos];
    const [p] = next.splice(from, 1);
    next.splice(target, 0, p);
    this.photos = next;
  }

  // ── Visibility ─────────────────────────────────────────────────────────────
  setLive(live: boolean): void {
    this.form.patchValue({ isActive: live, status: live ? 2 : 1 });
  }

  get categoryName(): string {
    return this.categoryOptions.find(c => c.value === this.form.value.categoryId)?.displayText ?? 'Category';
  }

  // ── Save ───────────────────────────────────────────────────────────────────
  saveAsDraft(): void {
    this.setLive(false);
    this.save();
  }

  save(): void {
    this.showErrors = true;
    this.form.markAllAsTouched();

    if (this.form.invalid || this.discountInvalid) {
      this.scrollTo(this.sectionDone('basic') ? 'pricing' : 'basic');
      this.message.warning('Fill in the highlighted fields first.');
      return;
    }
    if (this.uploading > 0) {
      this.message.info('Wait for the photos to finish uploading.');
      return;
    }

    const v = this.form.value;
    const payload = {
      ...v,
      name:          v.name?.trim(),
      slug:          this.slugify(v.slug || v.name || ''),
      discountPrice: this.hasDiscount ? this.discount : null,
      weight:        v.weight ? `${v.weight} kg` : null,
      primaryImage:  this.photos[0]?.url ?? null,
      // The main photo lives in PrimaryImage; the gallery carries the rest
      images: this.photos.slice(1).map((p, i) => ({
        id:           p.id,
        imageUrl:     p.url,
        displayOrder: i,
        isActive:     true,
      })),
      bundle: false,
    };

    this.saving = true;
    const request$ = this.isEdit
      ? this.productSvc.update(this.input!.id, payload)
      : this.productSvc.create(payload);

    request$.subscribe({
      next: () => {
        this.message.success(this.isEdit ? 'Product updated.' : 'Product created.');
        this.saving = false;
        this.handleProductSaved.emit();
      },
      error: err => {
        this.saving = false;
        this.message.error(err?.error?.error?.message || 'Something went wrong. Please try again.');
      },
    });
  }

  close(): void {
    this.onProductEntryDrawerClosed.emit();
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  private slugify(s: string): string {
    return s.toLowerCase().trim()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 120);
  }

  /** Older records store weight as free text like "22kg" or "27.5 kg". */
  private parseWeight(w?: string): number | null {
    const n = parseFloat(w ?? '');
    return isNaN(n) ? null : n;
  }
}
