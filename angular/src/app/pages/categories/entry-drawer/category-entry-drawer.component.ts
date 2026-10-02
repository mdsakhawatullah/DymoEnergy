import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { CategoryService } from '../../../proxy/categories/category.service';
import { CategoryDto, CategoryLayoutTypeLabels } from '../../../proxy/categories/models';
import { ImageUploadService } from '../../../proxy/image-upload/image-upload.service';

@Component({
  selector:    'category-entry-drawer',
  templateUrl: './category-entry-drawer.component.html',
  styleUrl:    './category-entry-drawer.component.css',
  imports:     [SharedModule],
})
export class CategoryEntryDrawerComponent implements OnInit, OnChanges, OnDestroy {

  @Input()  input: CategoryDto | null = null;

  @Output() onCategoryEntryDrawerClosed = new EventEmitter<void>();
  @Output() handleCategorySaved         = new EventEmitter<void>();

  form!: FormGroup;
  saving           = false;
  publishNow       = false;
  uploadingBg      = false;
  uploadingThumb   = false;
  showSeo          = false;

  layoutOptions = Object.entries(CategoryLayoutTypeLabels).map(([value, label]) => ({
    value: +value,
    label,
  }));

  constructor(
    private fb:              FormBuilder,
    private categorySvc:    CategoryService,
    private message:         NzMessageService,
    private imageUploadSvc:  ImageUploadService,
  ) {
    this.buildForm();
  }

  private destroy$ = new Subject<void>();
  /** Once the slug is edited by hand (or loaded from a saved category) stop deriving it from the name. */
  private slugTouched = false;

  get isEdit(): boolean { return !!this.input?.id; }
  get statusLabel(): string { return this.form.get('isPublished')?.value ? 'Published' : 'Draft'; }

  ngOnInit(): void {
    this.form.get('name')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(name => {
      if (!this.isEdit && !this.slugTouched) this.form.get('slug')!.setValue(this.slugify(name), { emitEvent: false });
    });
    this.form.get('slug')!.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.slugTouched = true);
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  private slugify(v: string | null): string {
    return (v ?? '').toLowerCase().trim().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) {
        this.slugTouched = true;
        this.publishNow = this.input.isPublished;
        this.showSeo = !!(this.input.metaTitle || this.input.metaDescription || this.input.metaKeywords);
        this.form.patchValue({
          name:                      this.input.name,
          slug:                      this.input.slug,
          description:               this.input.description,
          heroTitle:                 this.input.heroTitle,
          heroSubtitle:              this.input.heroSubtitle,
          thumbnailImageUrl:         this.input.thumbnailImageUrl,
          primaryBackgroundImageUrl: this.input.primaryBackgroundImageUrl,
          layoutType:                this.input.layoutType,
          isPublished:               this.input.isPublished,
          isFeatured:                this.input.isFeatured,
          displayOrder:              this.input.displayOrder,
          metaTitle:                 this.input.metaTitle,
          metaDescription:           this.input.metaDescription,
          metaKeywords:              this.input.metaKeywords,
        });
      } else {
        this.slugTouched = false;
        this.publishNow = false;
        this.form.reset({ layoutType: 1, isPublished: false, isFeatured: false, displayOrder: 0 }, { emitEvent: false });
      }
    }
  }

  buildForm(): void {
    this.form = this.fb.group({
      name:                      [null],
      slug:                      [null],
      description:               [null],
      heroTitle:                 [null],
      heroSubtitle:              [null],
      thumbnailImageUrl:         [null],
      primaryBackgroundImageUrl: [null],
      layoutType:                [1],
      isPublished:               [false],
      isFeatured:                [false],
      displayOrder:              [0],
      metaTitle:                 [null],
      metaDescription:           [null],
      metaKeywords:              [null],
    });
  }

  onPublishNowChange(checked: boolean): void {
    this.publishNow = checked;
    this.form.patchValue({ isPublished: checked });
  }

  triggerFileInput(inputId: string): void {
    document.getElementById(inputId)?.click();
  }

  onBgImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.uploadingBg = true;
    this.imageUploadSvc.uploadImage(file, 'categories/backgrounds').subscribe({
      next: url => {
        this.form.patchValue({ primaryBackgroundImageUrl: url });
        this.uploadingBg = false;
        this.message.success('Background image uploaded!');
      },
      error: () => {
        this.uploadingBg = false;
        this.message.error('Failed to upload background image.');
      },
    });
  }

  onThumbImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.uploadingThumb = true;
    this.imageUploadSvc.uploadImage(file, 'categories/thumbnails').subscribe({
      next: url => {
        this.form.patchValue({ thumbnailImageUrl: url });
        this.uploadingThumb = false;
        this.message.success('Thumbnail uploaded!');
      },
      error: () => {
        this.uploadingThumb = false;
        this.message.error('Failed to upload thumbnail.');
      },
    });
  }

  save(): void {
    if (!this.form.value.name?.trim()) { this.message.warning('Enter a category name.'); return; }
    this.saving = true;
    const v = this.form.value;
    const payload = { ...v, name: v.name.trim(), slug: v.slug?.trim() || this.slugify(v.name), images: [], overlayOpacity: 0.4 };

    const request$ = this.input?.id
      ? this.categorySvc.update(this.input.id, payload)
      : this.categorySvc.create(payload);

    request$.subscribe({
      next: () => {
        this.message.success(this.input?.id ? 'Category updated.' : 'Category created.');
        this.saving = false;
        this.handleCategorySaved.emit();
      },
      error: () => {
        this.message.error('Something went wrong. Please try again.');
        this.saving = false;
      },
    });
  }

  close(): void {
    this.onCategoryEntryDrawerClosed.emit();
  }
}
