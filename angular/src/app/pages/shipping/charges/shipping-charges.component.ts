import { Component, Input, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ShippingConfigService } from '../../../proxy/shipping/shipping-config.service';
import { ShippingService } from '../../../proxy/shipping/shipping.service';
import { PathaoLocationDto } from '../../../proxy/shipping/models';
import { Observable, of, switchMap } from 'rxjs';
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

  constructor(private api: ShippingConfigService, private shipping: ShippingService, private message: NzMessageService) {}

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
    return {
      name: '', note: '', charge: 0, perExtraKg: 0, courierCost: 0, days: '', order: 0,
      pathaoCityId: null as number | null, pathaoZoneId: null as number | null,
    };
  }

  // Pathao locations for the zone drawer, loaded once per page.
  cities: PathaoLocationDto[] = [];
  areas: PathaoLocationDto[] = [];
  citiesLoading = false;
  areasLoading = false;
  pathaoError = '';
  refreshing = false;
  refreshingId: number | null = null;

  startZone(z?: ShippingZoneDto): void {
    this.zoneId = z?.id ?? null;
    this.zone = z
      ? {
          name: z.name, note: z.note ?? '', charge: z.charge, perExtraKg: z.perExtraKg, courierCost: z.courierCost, days: z.days ?? '', order: z.order,
          pathaoCityId: z.pathaoCityId ?? null, pathaoZoneId: z.pathaoZoneId ?? null,
        }
      : this.blankZone();
    // Show the saved names straight away, before Pathao's lists arrive.
    this.areas = z?.pathaoZoneId ? [{ id: z.pathaoZoneId, name: z.pathaoZoneName ?? '' }] : [];
    if (z?.pathaoCityId && !this.cities.some(c => c.id === z.pathaoCityId)) this.cities = [...this.cities, { id: z.pathaoCityId, name: z.pathaoCityName ?? '' }];
    this.zoneOpen = true;
    this.loadCities();
    if (z?.pathaoCityId) this.loadAreas(z.pathaoCityId, true);
  }

  private loadCities(): void {
    const id = this.page?.pathaoAccountId;
    if (!id || this.cities.length > 1 || this.citiesLoading) return;
    this.citiesLoading = true;
    this.pathaoError = '';
    this.shipping.getPathaoCities(id).subscribe({
      next: list => { this.citiesLoading = false; this.cities = list.sort((a, b) => a.name.localeCompare(b.name)); },
      error: e => { this.citiesLoading = false; this.pathaoError = e?.error?.error?.message ?? 'Could not reach Pathao. Check its keys on Couriers & keys.'; },
    });
  }

  private loadAreas(cityId: number, keep = false): void {
    const id = this.page?.pathaoAccountId;
    if (!id) return;
    this.areasLoading = true;
    if (!keep) { this.areas = []; this.zone.pathaoZoneId = null; }
    this.shipping.getPathaoZones(id, cityId).subscribe({
      next: list => { this.areasLoading = false; this.areas = list.sort((a, b) => a.name.localeCompare(b.name)); },
      error: () => (this.areasLoading = false),
    });
  }

  cityChanged(cityId: number | null): void {
    if (cityId) this.loadAreas(cityId);
    else { this.areas = []; this.zone.pathaoZoneId = null; }
  }

  get zoneMargin(): number {
    return (this.zone.charge || 0) - (this.zone.courierCost || 0);
  }

  private zoneInput() {
    const z = this.zone;
    return {
      name: z.name.trim(), note: z.note.trim() || null, days: z.days.trim() || null, order: z.order,
      charge: z.charge || 0, perExtraKg: z.perExtraKg || 0, courierCost: z.courierCost || 0,
      pathaoCityId: z.pathaoZoneId ? z.pathaoCityId : null, pathaoCityName: z.pathaoZoneId ? this.cities.find(c => c.id === z.pathaoCityId)?.name ?? null : null,
      pathaoZoneId: z.pathaoZoneId, pathaoZoneName: z.pathaoZoneId ? this.areas.find(a => a.id === z.pathaoZoneId)?.name ?? null : null,
    };
  }

  /** Saves the zone; with <paramref name="price"/> also asks Pathao for its price right after. */
  saveZone(price = false): void {
    if (!this.zone.name.trim()) return void this.message.warning('Give the zone a name.');
    if (price && !this.zone.pathaoZoneId) return void this.message.warning('Choose a Pathao city and zone first.');
    this.zoneSaving = true;
    const call: Observable<ShippingZoneDto> = this.zoneId ? this.api.updateZone(this.zoneId, this.zoneInput()) : this.api.createZone(this.zoneInput());
    call.pipe(switchMap(saved => (price ? this.api.refreshZonePrices(saved.id) : of(null)))).subscribe({
      next: r => {
        this.zoneSaving = false;
        this.zoneOpen = false;
        const res = r?.zones[0];
        if (res) res.ok ? this.message.success(`${res.name}: Pathao charges ${res.message}.`) : this.message.error(`${res.name}: ${res.message}`, { nzDuration: 8000 });
        this.load();
      },
      error: () => (this.zoneSaving = false),
    });
  }

  get linkedCount(): number {
    return (this.page?.zones ?? []).filter(z => z.pathaoZoneId).length;
  }

  refreshPrices(z?: ShippingZoneDto): void {
    this.refreshing = !z;
    this.refreshingId = z?.id ?? null;
    this.api.refreshZonePrices(z?.id).subscribe({
      next: r => {
        this.refreshing = false;
        this.refreshingId = null;
        const ok = r.zones.filter(x => x.ok).length;
        const bad = r.zones.filter(x => !x.ok && x.message !== 'No Pathao location chosen.');
        if (ok) this.message.success(`${ok} ${ok === 1 ? 'price' : 'prices'} updated from ${r.source}.${r.codFeePercent != null ? ` Pathao's cash fee: ${r.codFeePercent}%.` : ''}`, { nzDuration: 6000 });
        if (bad.length) this.message.error(bad.map(x => `${x.name}: ${x.message}`).join('  •  '), { nzDuration: 10000 });
        if (!ok && !bad.length) this.message.info('No zone is linked to a Pathao location yet. Edit a zone and choose one.');
        this.load();
      },
      error: () => { this.refreshing = false; this.refreshingId = null; },
    });
  }

  deleteZone(z: ShippingZoneDto): void {
    this.api.deleteZone(z.id).subscribe(() => { this.message.success(`${z.name} removed.`); this.load(); });
  }

  moveZone(index: number, step: -1 | 1): void {
    const zones = this.page?.zones ?? [];
    const a = zones[index], b = zones[index + step];
    if (!a || !b) return;
    // Send everything back, including the Pathao link, so reordering never clears it.
    const dto = (z: ShippingZoneDto, order: number) => ({
      name: z.name, note: z.note, charge: z.charge, perExtraKg: z.perExtraKg, courierCost: z.courierCost, days: z.days, order,
      pathaoCityId: z.pathaoCityId, pathaoCityName: z.pathaoCityName, pathaoZoneId: z.pathaoZoneId, pathaoZoneName: z.pathaoZoneName,
    });
    const orderA = a.order === b.order ? index + 1 : a.order;
    const orderB = a.order === b.order ? index + 1 + step : b.order;
    this.api.updateZone(a.id, dto(a, orderB)).subscribe(() => this.api.updateZone(b.id, dto(b, orderA)).subscribe(() => this.load()));
  }
}
