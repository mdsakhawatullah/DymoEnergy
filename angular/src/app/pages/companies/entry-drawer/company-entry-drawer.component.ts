import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { CompanyService } from '../../../proxy/companies/company.service';
import { CompanyDto, CompanyStatusLabels } from '../../../proxy/companies/models';
import { SelectListDto } from '../../../proxy/catalogues/models';

@Component({
  selector:    'company-entry-drawer',
  templateUrl: './company-entry-drawer.component.html',
  styleUrl:    './company-entry-drawer.component.css',
  imports:     [SharedModule],
})
export class CompanyEntryDrawerComponent implements OnChanges, OnInit {

  @Input()  input: CompanyDto | null = null;

  @Output() onCompanyEntryDrawerClosed = new EventEmitter<void>();
  @Output() handleCompanySaved         = new EventEmitter<void>();

  form!: FormGroup;
  saving = false;
  parentCompanyOptions: SelectListDto[] = [];

  statusOptions = Object.entries(CompanyStatusLabels).map(([value, label]) => ({
    value: +value,
    label,
  }));

  constructor(
    private fb:         FormBuilder,
    private companySvc: CompanyService,
    private message:    NzMessageService,
  ) {
    this.buildForm();
  }

  ngOnInit(): void {
    this.companySvc.getSelectList().subscribe({
      next: items => this.parentCompanyOptions = items.filter(o => o.value !== this.input?.id),
      error: () => this.message.warning('Could not load parent companies.'),
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) {
        this.form.patchValue({
          companyName:           this.input.companyName,
          companyCode:           this.input.companyCode,
          email:                 this.input.email,
          phone:                 this.input.phone,
          website:               this.input.website,
          taxId:                 this.input.taxId,
          address:               this.input.address,
          city:                  this.input.city,
          state:                 this.input.state,
          postalCode:            this.input.postalCode,
          country:               this.input.country,
          contactPersonName:     this.input.contactPersonName,
          contactPersonEmail:    this.input.contactPersonEmail,
          contactPersonPhone:    this.input.contactPersonPhone,
          notes:                 this.input.notes,
          status:                this.input.status,
          sendStatementTo:       this.input.sendStatementTo,
          isParentCompany:       this.input.isParentCompany,
          hasChildCompany:       this.input.hasChildCompany,
          parentCompanyId:       this.input.parentCompanyId,
          defaultShippingFee:    this.input.defaultShippingFee,
          freeShippingThreshold: this.input.freeShippingThreshold,
          isFlatRateShipping:    this.input.isFlatRateShipping,
          shippingCurrencyCode:  this.input.shippingCurrencyCode,
          externalAccountId:     this.input.externalAccountId,
          apiKey:                this.input.apiKey,
          webhookUrl:            this.input.webhookUrl,
          isSyncEnabled:         this.input.isSyncEnabled,
        });
      } else {
        this.form.reset({
          status: 1,
          isParentCompany: false,
          hasChildCompany: false,
          defaultShippingFee: 0,
          isFlatRateShipping: false,
          shippingCurrencyCode: 'BDT',
          isSyncEnabled: false,
        });
      }
    }
  }

  buildForm(): void {
    this.form = this.fb.group({
      companyName:           [null],
      companyCode:           [null],
      email:                 [null],
      phone:                 [null],
      website:               [null],
      taxId:                 [null],
      address:               [null],
      city:                  [null],
      state:                 [null],
      postalCode:            [null],
      country:               [null],
      contactPersonName:     [null],
      contactPersonEmail:    [null],
      contactPersonPhone:    [null],
      notes:                 [null],
      status:                [1],
      sendStatementTo:       [null],
      isParentCompany:       [false],
      hasChildCompany:       [false],
      parentCompanyId:       [null],
      defaultShippingFee:    [0],
      freeShippingThreshold: [null],
      isFlatRateShipping:    [false],
      shippingCurrencyCode:  ['BDT'],
      externalAccountId:     [null],
      apiKey:                [null],
      webhookUrl:            [null],
      isSyncEnabled:         [false],
    });
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const payload = { ...this.form.value };

    const request$ = this.input?.id
      ? this.companySvc.update(this.input.id, payload)
      : this.companySvc.create(payload);

    request$.subscribe({
      next: () => {
        this.message.success(this.input?.id ? 'Company updated.' : 'Company created.');
        this.saving = false;
        this.handleCompanySaved.emit();
      },
      error: () => {
        this.message.error('Something went wrong. Please try again.');
        this.saving = false;
      },
    });
  }

  close(): void {
    this.onCompanyEntryDrawerClosed.emit();
  }
}
