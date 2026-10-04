import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { StockService } from '../../../proxy/stock/stock.service';
import { StockSupplierDto, WarehouseDto } from '../../../proxy/stock/models';

/** Drawer to add and edit warehouses and suppliers. */
@Component({
  selector: 'app-stock-places',
  templateUrl: './stock-places.component.html',
  styleUrls: ['../stock.css', './stock-places.component.css'],
  imports: [SharedModule],
})
export class StockPlacesComponent {
  @Input() warehouses: WarehouseDto[] = [];
  @Input() suppliers: StockSupplierDto[] = [];
  @Input() canEdit = false;
  @Output() closed = new EventEmitter<void>();
  @Output() changed = new EventEmitter<void>();

  tab: 'warehouses' | 'suppliers' = 'warehouses';
  saving = false;

  whId: number | null = null;
  wh: { name: string; shortCode: string; address: string; isDefault: boolean; isActive: boolean } | null = null;

  supId: number | null = null;
  sup: { name: string; phone: string; email: string; address: string; note: string; isActive: boolean } | null = null;

  constructor(private api: StockService, private message: NzMessageService) {}

  editWarehouse(w?: WarehouseDto): void {
    this.whId = w?.id ?? null;
    this.wh = { name: w?.name ?? '', shortCode: w?.shortCode ?? '', address: w?.address ?? '', isDefault: w?.isDefault ?? false, isActive: w?.isActive ?? true };
  }

  saveWarehouse(): void {
    const m = this.wh;
    if (!m) return;
    if (!m.name.trim() || !m.shortCode.trim()) return void this.message.warning('A warehouse needs a name and a short code.');
    const order = this.warehouses.find(w => w.id === this.whId)?.order ?? 0;
    const input = { name: m.name.trim(), shortCode: m.shortCode.trim(), address: m.address.trim() || null, isDefault: m.isDefault, isActive: m.isActive, order };
    this.saving = true;
    (this.whId ? this.api.updateWarehouse(this.whId, input) : this.api.createWarehouse(input)).subscribe({
      next: saved => {
        this.saving = false;
        this.wh = null;
        const i = this.warehouses.findIndex(w => w.id === saved.id);
        if (saved.isDefault) this.warehouses.forEach(w => (w.isDefault = false));
        if (i >= 0) this.warehouses[i] = saved; else this.warehouses.push(saved);
        this.message.success(`${saved.name} saved.`);
        this.changed.emit();
      },
      error: () => (this.saving = false),
    });
  }

  contact(s: StockSupplierDto): string {
    return [s.phone, s.email].filter(Boolean).join(' · ') || 'no contact saved';
  }

  editSupplier(s?: StockSupplierDto): void {
    this.supId = s?.id ?? null;
    this.sup = { name: s?.name ?? '', phone: s?.phone ?? '', email: s?.email ?? '', address: s?.address ?? '', note: s?.note ?? '', isActive: s?.isActive ?? true };
  }

  saveSupplier(): void {
    const m = this.sup;
    if (!m) return;
    if (!m.name.trim()) return void this.message.warning('Give the supplier a name.');
    const t = (v: string) => v.trim() || null;
    const input = { name: m.name.trim(), phone: t(m.phone), email: t(m.email), address: t(m.address), note: t(m.note), isActive: m.isActive };
    this.saving = true;
    (this.supId ? this.api.updateSupplier(this.supId, input) : this.api.createSupplier(input)).subscribe({
      next: saved => {
        this.saving = false;
        this.sup = null;
        const i = this.suppliers.findIndex(s => s.id === saved.id);
        if (i >= 0) this.suppliers[i] = saved; else this.suppliers.push(saved);
        this.message.success(`${saved.name} saved.`);
        this.changed.emit();
      },
      error: () => (this.saving = false),
    });
  }
}
