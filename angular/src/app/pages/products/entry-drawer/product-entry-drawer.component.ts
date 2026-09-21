import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ProductService } from '../../../proxy/products/product.service';
import { ProductDto, ProductStatusLabels } from '../../../proxy/products/models';
import { CatalogueService } from '../../../proxy/catalogues/catalogue.service';
import { SelectListDto } from '../../../proxy/catalogues/models';
import { ImageUploadService } from '../../../proxy/image-upload/image-upload.service';

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
  saving = false;
  uploadingPrimaryImage = false;
  catalogueOptions: SelectListDto[] = [];

  statusOptions = Object.entries(ProductStatusLabels).map(([value, label]) => ({
    value: +value,
    label,
  }));

  constructor(
    private fb:           FormBuilder,
    private productSvc:   ProductService,
    private catalogueSvc: CatalogueService,
    private message:      NzMessageService,
    private imageUploadSvc: ImageUploadService,
  ) {
    this.buildForm();
  }

  ngOnInit(): void {
    this.catalogueSvc.getSelectList().subscribe({
      next: items => this.catalogueOptions = items,
      error: () => this.message.warning('Could not load catalogues.'),
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) {
        this.form.patchValue({
          catalogueId:   this.input.catalogueId,
          name:          this.input.name,
          slug:          this.input.slug,
          summary:       this.input.summary,
          sku:           this.input.sku,
          price:         this.input.price,
          discountPrice: this.input.discountPrice,
          weight:        this.input.weight,
          stockQuantity: this.input.stockQuantity,
          primaryImage:  this.input.primaryImage,
          description:   this.input.description,
          status:        this.input.status,
          isActive:      this.input.isActive,
          isFeatured:    this.input.isFeatured,
          displayOrder:  this.input.displayOrder,
          metaTitle:     this.input.metaTitle,
          metaDescription: this.input.metaDescription,
          metaKeywords:  this.input.metaKeywords,
        });
      } else {
        this.form.reset({ status: 1, isActive: false, isFeatured: false, displayOrder: 0, stockQuantity: 0, price: 0, catalogueId: null });
      }
    }
  }

  buildForm(): void {
    this.form = this.fb.group({
      catalogueId:     [null],
      name:            [null],
      slug:            [null],
      summary:         [null],
      sku:             [null],
      price:           [0],
      discountPrice:   [null],
      weight:          [null],
      stockQuantity:   [0],
      primaryImage:    [null],
      description:     [null],
      status:          [1],
      isActive:        [false],
      isFeatured:      [false],
      displayOrder:    [0],
      metaTitle:       [null],
      metaDescription: [null],
      metaKeywords:    [null],
    });

    // STATUS mirrors the ACTIVE switch: Active when on, Draft when off.
    this.form.get('isActive')!.valueChanges.subscribe((isActive: boolean) => {
      this.form.get('status')!.setValue(isActive ? 2 : 1); // 2 = Active, 1 = Draft
    });
  }

  triggerFileInput(inputId: string): void {
    document.getElementById(inputId)?.click();
  }

  onPrimaryImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.uploadingPrimaryImage = true;
    this.imageUploadSvc.uploadImage(file, 'products').subscribe({
      next: url => {
        this.form.patchValue({ primaryImage: url });
        this.uploadingPrimaryImage = false;
        this.message.success('Image uploaded!');
      },
      error: () => {
        this.uploadingPrimaryImage = false;
        this.message.error('Failed to upload image.');
      },
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const payload = { ...this.form.value, images: [], bundle: false };

    const request$ = this.input?.id
      ? this.productSvc.update(this.input.id, payload)
      : this.productSvc.create(payload);

    request$.subscribe({
      next: () => {
        this.message.success(this.input?.id ? 'Product updated.' : 'Product created.');
        this.saving = false;
        this.handleProductSaved.emit();
      },
      error: () => {
        this.message.error('Something went wrong. Please try again.');
        this.saving = false;
      },
    });
  }

  close(): void {
    this.onProductEntryDrawerClosed.emit();
  }
}
