import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ProductService } from '../../../proxy/products/product.service';
import { ProductDto, ProductStatusLabels } from '../../../proxy/products/models';

@Component({
  selector:    'product-entry-drawer',
  templateUrl: './product-entry-drawer.component.html',
  styleUrl:    './product-entry-drawer.component.css',
  imports:     [SharedModule],
})
export class ProductEntryDrawerComponent implements OnChanges {

  @Input()  input: ProductDto | null = null;

  @Output() onProductEntryDrawerClosed = new EventEmitter<void>();
  @Output() handleProductSaved         = new EventEmitter<void>();

  form!: FormGroup;
  saving = false;

  statusOptions = Object.entries(ProductStatusLabels).map(([value, label]) => ({
    value: +value,
    label,
  }));

  constructor(
    private fb:         FormBuilder,
    private productSvc: ProductService,
    private message:    NzMessageService,
  ) {
    this.buildForm();
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
