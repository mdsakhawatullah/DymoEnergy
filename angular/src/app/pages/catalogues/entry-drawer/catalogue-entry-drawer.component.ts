import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { CatalogueService } from '../../../proxy/catalogues/catalogue.service';
import { CatalogueDto, CatalogueLayoutTypeLabels } from '../../../proxy/catalogues/models';

@Component({
  selector:    'catalogue-entry-drawer',
  templateUrl: './catalogue-entry-drawer.component.html',
  styleUrl:    './catalogue-entry-drawer.component.css',
  imports:     [SharedModule],
})
export class CatalogueEntryDrawerComponent implements OnChanges {

  @Input()  input: CatalogueDto | null = null;

  @Output() onCatalogueEntryDrawerClosed = new EventEmitter<void>();
  @Output() handleCatalogueSaved         = new EventEmitter<void>();

  form!: FormGroup;
  saving        = false;
  publishNow    = false;

  layoutOptions = Object.entries(CatalogueLayoutTypeLabels).map(([value, label]) => ({
    value: +value,
    label,
  }));

  constructor(
    private fb:           FormBuilder,
    private catalogueSvc: CatalogueService,
    private message:      NzMessageService,
  ) {
    this.buildForm();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) {
        this.publishNow = this.input.isPublished;
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
        this.publishNow = false;
        this.form.reset({ layoutType: 1, isPublished: false, isFeatured: false, displayOrder: 0 });
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

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const payload = { ...this.form.value, images: [], overlayOpacity: 0.4 };

    const request$ = this.input?.id
      ? this.catalogueSvc.update(this.input.id, payload)
      : this.catalogueSvc.create(payload);

    request$.subscribe({
      next: () => {
        this.message.success(this.input?.id ? 'Catalogue updated.' : 'Catalogue created.');
        this.saving = false;
        this.handleCatalogueSaved.emit();
      },
      error: () => {
        this.message.error('Something went wrong. Please try again.');
        this.saving = false;
      },
    });
  }

  close(): void {
    this.onCatalogueEntryDrawerClosed.emit();
  }
}
