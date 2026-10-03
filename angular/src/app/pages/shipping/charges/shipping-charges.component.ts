import { Component, Input, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ShippingConfigService } from '../../../proxy/shipping/shipping-config.service';
import { ChargesPageDto, ShippingItemKind, ShippingSettingDto, ShippingZoneDto } from '../../../proxy/shipping/config.models';
import { fmtFull } from '../../budgets-costs/finance.utils';
import { ShippingItemListComponent } from '../item-list/shipping-item-list.component';

@Component({
  selector: 'app-shipping-charges',
  templateUrl: './shipping-charges.component.html',
  styleUrls: ['../shipping.component.css', './shipping-charges.component.css'],
  imports: [SharedModule, ShippingItemListComponent],
})
export class ShippingChargesComponent implements OnInit {
  @Input() canEdit = false;

  readonly Kind = ShippingItemKind;
  page: ChargesPageDto | null = null;
  failed = false;

  setting: ShippingSettingDto | null = null;
  settingDirty = false;
  savingSetting = false;

  zoneOpen = false;
  zoneSaving = false;
  zoneId: number | null = null;
  zone = this.blankZone();

  constructor(private api: ShippingConfigService, private message: NzMessageService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed = false;
    this.api.getCharges().subscribe({
      next: p => {
        this.page = p;
        if (!this.settingDirty) this.setting = { ...p.setting };
      },
      error: () => (this.failed = true),
    });
  }

  money = (v: number) => fmtFull(v, '৳');

  /** Positive margins are kept, negative ones lost on every parcel. */
  marginText(z: ShippingZoneDto): string {
    if (z.margin === 0) return 'break even';
    return (z.margin > 0 ? '+' : '−') + this.money(Math.abs(z.margin));
  }

  get losingZones(): ShippingZoneDto[] {
    return (this.page?.zones ?? []).filter(z => z.margin < 0);
  }

  get losingNames(): string {
    return this.losingZones.map(z => z.name).join(', ');
  }

  // ── Checkout rules ──────────────────────────────────────────────────────
  touch(): void {
    this.settingDirty = true;
  }

  resetSetting(): void {
    if (!this.page) return;
    this.setting = { ...this.page.setting };
    this.settingDirty = false;
  }

  saveSetting(): void {
    if (!this.setting) return;
    this.savingSetting = true;
    this.api.updateSetting(this.setting).subscribe({
      next: s => {
        this.savingSetting = false;
        this.settingDirty = false;
        if (this.page) this.page.setting = s;
        this.setting = { ...s };
        this.message.success('Checkout rules saved.');
      },
      error: () => (this.savingSetting = false),
    });
  }

  // ── Zones ───────────────────────────────────────────────────────────────
  private blankZone() {
    return { name: '', note: '', charge: 0, perExtraKg: 0, courierCost: 0, days: '', order: 0 };
  }

  startZone(z?: ShippingZoneDto): void {
    this.zoneId = z?.id ?? null;
    this.zone = z
      ? { name: z.name, note: z.note ?? '', charge: z.charge, perExtraKg: z.perExtraKg, courierCost: z.courierCost, days: z.days ?? '', order: z.order }
      : this.blankZone();
    this.zoneOpen = true;
  }

  get zoneMargin(): number {
    return (this.zone.charge || 0) - (this.zone.courierCost || 0);
  }

  saveZone(): void {
    if (!this.zone.name.trim()) return void this.message.warning('Give the zone a name.');
    const input = { ...this.zone, name: this.zone.name.trim(), note: this.zone.note.trim() || null, days: this.zone.days.trim() || null };
    this.zoneSaving = true;
    const call = this.zoneId ? this.api.updateZone(this.zoneId, input) : this.api.createZone(input);
    call.subscribe({
      next: () => { this.zoneSaving = false; this.zoneOpen = false; this.load(); },
      error: () => (this.zoneSaving = false),
    });
  }

  deleteZone(z: ShippingZoneDto): void {
    this.api.deleteZone(z.id).subscribe(() => { this.message.success(`${z.name} removed.`); this.load(); });
  }

  moveZone(index: number, step: -1 | 1): void {
    const zones = this.page?.zones ?? [];
    const a = zones[index], b = zones[index + step];
    if (!a || !b) return;
    const dto = (z: ShippingZoneDto, order: number) =>
      ({ name: z.name, note: z.note, charge: z.charge, perExtraKg: z.perExtraKg, courierCost: z.courierCost, days: z.days, order });
    const orderA = a.order === b.order ? index + 1 : a.order;
    const orderB = a.order === b.order ? index + 1 + step : b.order;
    this.api.updateZone(a.id, dto(a, orderB)).subscribe(() => this.api.updateZone(b.id, dto(b, orderA)).subscribe(() => this.load()));
  }
}
