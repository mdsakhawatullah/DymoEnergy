import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { CustomerService } from '../../../proxy/customers/customer.service';
import {
  CUSTOMER_SOURCES,
  CUSTOMER_STATUSES,
  CUSTOMER_TYPES,
  CreateUpdateCustomerDto,
  CustomerSource,
  CustomerStatus,
  CustomerType,
} from '../../../proxy/customers/models';

@Component({
  selector: 'app-customer-entry-drawer',
  templateUrl: './customer-entry-drawer.component.html',
  styleUrls: ['./customer-entry-drawer.component.css'],
  imports: [SharedModule],
})
export class CustomerEntryDrawerComponent implements OnChanges {
  /** 0 for a new customer, otherwise the one being edited. */
  @Input({ required: true }) customerId!: number;
  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  readonly Type = CustomerType;
  readonly types = CUSTOMER_TYPES;
  readonly statuses = CUSTOMER_STATUSES;
  readonly sources = CUSTOMER_SOURCES;

  m: CreateUpdateCustomerDto = this.blank();
  loading = false;
  saving = false;
  showErrors = false;
  /** Only shown while editing someone who already has unattached orders. */
  linkedCount = 0;

  constructor(private api: CustomerService, private message: NzMessageService) {}

  private blank(): CreateUpdateCustomerDto {
    return {
      name: '', phone: null, email: null, type: CustomerType.Household, status: CustomerStatus.Active,
      source: CustomerSource.Showroom, companyName: null, taxId: null, address: null, area: null, city: null,
      district: null, assignedTo: null, note: null, tags: null, creditLimit: 0, paymentTermDays: 0,
      linkMatchingOrders: true,
    };
  }

  ngOnChanges(): void {
    this.showErrors = false;
    this.m = this.blank();
    if (!this.customerId) return;

    this.loading = true;
    this.api.get(this.customerId).subscribe({
      next: c => {
        this.loading = false;
        this.linkedCount = c.looseOrders;
        this.m = {
          name: c.name, phone: c.phone, email: c.email, type: c.type, status: c.status, source: c.source,
          companyName: c.companyName, taxId: c.taxId, address: c.address, area: c.area, city: c.city,
          district: c.district, assignedTo: c.assignedTo, note: c.note, tags: c.tags.join(', '),
          creditLimit: c.creditLimit, paymentTermDays: c.paymentTermDays, linkMatchingOrders: true,
        };
      },
      error: () => { this.loading = false; this.message.error('Could not load that customer.'); },
    });
  }

  get isNew(): boolean {
    return !this.customerId;
  }

  get needsCompany(): boolean {
    return this.m.type !== CustomerType.Household;
  }

  get nameInvalid(): boolean {
    return this.showErrors && !this.m.name.trim();
  }

  /** A phone that is not a Bangladeshi mobile cannot match old orders, so it is worth saying. */
  get phoneOdd(): boolean {
    const digits = (this.m.phone ?? '').replace(/\D/g, '');
    return digits.length > 0 && !/^(0|88 ?0?)?1\d{9}$/.test(digits);
  }

  save(): void {
    this.showErrors = true;
    if (!this.m.name.trim()) {
      this.message.warning('A customer needs a name.');
      return;
    }

    this.saving = true;
    const call = this.isNew ? this.api.create(this.m) : this.api.update(this.customerId, this.m);
    call.subscribe({
      next: c => {
        this.saving = false;
        const attached = c.orderCount > 0 && this.isNew
          ? ` ${c.orderCount} past ${c.orderCount === 1 ? 'order was' : 'orders were'} attached.`
          : '';
        this.message.success(`${c.name} saved.${attached}`);
        this.saved.emit();
      },
      error: e => {
        this.saving = false;
        this.message.error(e?.error?.error?.message ?? 'Could not save the customer.');
      },
    });
  }
}
