import { Component, OnInit } from '@angular/core';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { ShippingService } from '../../proxy/shipping/shipping.service';
import {
  CourierDetailDto,
  CourierEnvironment,
  CourierFieldDto,
  CourierLogDto,
  CourierProvider,
  CourierSummaryDto,
  PathaoStoreDto,
  ReadyOrderDto,
  SendParcelResultDto,
  ShipmentDto,
  ShipmentsPageDto,
  ShippingOverviewDto,
} from '../../proxy/shipping/models';
import { environment } from '../../../environments/environment';
import { tint } from '../project-planning/project-planning.utils';
import { fmtCompact, fmtFull } from '../budgets-costs/finance.utils';

type TabKey = 'couriers' | 'shipments' | 'charges' | 'cod' | 'rules';

const PERM = {
  send: 'DymoEnergy.Shipping.Send',
  edit: 'DymoEnergy.Shipping.Edit',
  keys: 'DymoEnergy.Shipping.ManageKeys',
};

@Component({
  selector: 'app-shipping',
  templateUrl: './shipping.component.html',
  styleUrl: './shipping.component.css',
  imports: [SharedModule],
})
export class ShippingComponent implements OnInit {
  readonly Env = CourierEnvironment;
  readonly Provider = CourierProvider;

  tab: TabKey = 'couriers';
  overview: ShippingOverviewDto | null = null;
  loading = false;

  // ── Couriers & keys ─────────────────────────────────────────────────────
  selectedId: number | null = null;
  detail: CourierDetailDto | null = null;
  /** Which set of keys is being viewed (can differ from the one in use). */
  keysEnv: CourierEnvironment = CourierEnvironment.Sandbox;
  editingKey: string | null = null;
  editValue = '';
  revealed: Record<string, string> = {};
  busy = false;
  testMessage: { ok: boolean; text: string } | null = null;

  stores: PathaoStoreDto[] = [];
  storesLoading = false;
  settings = { pickupStoreId: null as string | null, deliveryType: 48, itemType: 2, weight: 1, displayName: '', shortCode: '', color: '', isEnabled: true };

  addOpen = false;
  addModel = { provider: CourierProvider.Steadfast, displayName: '', shortCode: '', color: '#2563EB' };
  readonly providerOptions = [
    { value: CourierProvider.Pathao, label: 'Pathao (API built in)' },
    { value: CourierProvider.Steadfast, label: 'Steadfast (keys only for now)' },
    { value: CourierProvider.RedX, label: 'RedX (keys only for now)' },
    { value: CourierProvider.ECourier, label: 'eCourier (keys only for now)' },
    { value: CourierProvider.OwnDelivery, label: 'Own delivery team' },
    { value: CourierProvider.Pickup, label: 'Customer pickup' },
  ];

  // ── Shipments ───────────────────────────────────────────────────────────
  page: ShipmentsPageDto | null = null;
  selected = new Set<number>();
  sendCourierId: number | null = null;
  sending = false;
  sendResults: SendParcelResultDto[] = [];
  filter = '';
  refreshing = new Set<number>();

  canSend = false;
  canEdit = false;
  canKeys = false;

  constructor(
    private api: ShippingService,
    private message: NzMessageService,
    permissions: PermissionService,
  ) {
    this.canSend = permissions.getGrantedPolicy(PERM.send);
    this.canEdit = permissions.getGrantedPolicy(PERM.edit);
    this.canKeys = permissions.getGrantedPolicy(PERM.keys);
  }

  ngOnInit(): void {
    this.loadOverview(true);
  }

  // ── Loading ─────────────────────────────────────────────────────────────
  loadOverview(selectFirst = false): void {
    this.loading = true;
    this.api.getOverview().subscribe({
      next: o => {
        this.overview = o;
        this.loading = false;
        if (selectFirst && o.couriers.length) this.select(o.couriers[0]);
      },
      error: () => (this.loading = false),
    });
  }

  setTab(tab: TabKey): void {
    this.tab = tab;
    if (tab === 'shipments') this.loadShipments();
  }

  money = (v: number) => fmtFull(v, '৳');
  compact = (v: number) => fmtCompact(v, { currencySymbol: '৳', compactMoney: true });
  tint = tint;

  // ── Couriers ────────────────────────────────────────────────────────────
  select(c: CourierSummaryDto): void {
    this.selectedId = c.id;
    this.editingKey = null;
    this.revealed = {};
    this.testMessage = null;
    this.stores = [];
    this.api.getCourier(c.id).subscribe(d => this.applyDetail(d, true));
  }

  private applyDetail(d: CourierDetailDto, resetEnv = false): void {
    this.detail = d;
    if (resetEnv) this.keysEnv = d.activeEnvironment;
    this.settings = {
      pickupStoreId: d.pickupStoreId ?? null, deliveryType: d.defaultDeliveryType, itemType: d.defaultItemType, weight: d.defaultWeightKg,
      displayName: d.displayName, shortCode: d.shortCode, color: d.color, isEnabled: d.isEnabled,
    };
    if (d.pickupStoreId && !this.stores.length) this.stores = [{ storeId: d.pickupStoreId, storeName: d.pickupStoreName ?? d.pickupStoreId, isActive: true, isDefault: false }];
  }

  private refreshDetail(): void {
    if (this.selectedId == null) return;
    this.api.getCourier(this.selectedId).subscribe(d => this.applyDetail(d));
    this.loadOverview();
  }

  get keysForEnv(): CourierFieldDto[] {
    return this.detail?.environments.find(e => e.environment === this.keysEnv)?.fields ?? [];
  }

  envSaved(env: CourierEnvironment): boolean {
    return !!this.detail?.environments.find(e => e.environment === env)?.keysSaved;
  }

  statusDot(s: CourierSummaryDto): string {
    return s.status === 'connected' || s.status === 'always' ? '#0E6B3F' : s.status === 'keys' ? '#D97706' : '#C9CFC9';
  }

  setActiveEnv(env: CourierEnvironment): void {
    if (!this.detail || !this.canKeys || this.detail.activeEnvironment === env) return;
    this.api.setEnvironment(this.detail.id, env).subscribe(d => {
      this.applyDetail(d);
      this.keysEnv = env;
      this.message.success(`${d.displayName} now uses its ${env === CourierEnvironment.Live ? 'live' : 'sandbox'} keys.`);
      this.loadOverview();
    });
  }

  startEdit(f: CourierFieldDto): void {
    this.editingKey = f.key;
    this.editValue = f.isSecret ? '' : f.display ?? '';
  }

  saveKey(f: CourierFieldDto): void {
    if (!this.detail) return;
    this.busy = true;
    this.api.updateCredential(this.detail.id, this.keysEnv, f.key, this.editValue.trim() || null).subscribe({
      next: d => {
        this.busy = false;
        this.editingKey = null;
        delete this.revealed[f.key];
        this.applyDetail(d);
        this.loadOverview();
        this.message.success(`${f.label} saved.`);
      },
      error: () => (this.busy = false),
    });
  }

  reveal(f: CourierFieldDto): void {
    if (!this.detail) return;
    if (this.revealed[f.key] !== undefined) { delete this.revealed[f.key]; return; }
    this.api.reveal(this.detail.id, this.keysEnv, f.key).subscribe(v => {
      this.revealed[f.key] = v;
      this.refreshDetail();
    });
  }

  copy(text: string | null | undefined, what: string): void {
    if (!text) return;
    navigator.clipboard.writeText(text).then(() => this.message.success(`${what} copied.`), () => this.message.error('Could not copy.'));
  }

  copyField(f: CourierFieldDto): void {
    if (f.isSecret) {
      if (this.revealed[f.key] !== undefined) this.copy(this.revealed[f.key], f.label);
      else this.message.info('Reveal it first to copy a secret.');
    } else this.copy(f.display, f.label);
  }

  test(): void {
    if (!this.detail) return;
    this.busy = true;
    this.testMessage = null;
    this.api.test(this.detail.id).subscribe({
      next: r => { this.busy = false; this.testMessage = { ok: r.ok, text: r.message }; this.refreshDetail(); },
      error: () => (this.busy = false),
    });
  }

  newToken(): void {
    if (!this.detail) return;
    this.busy = true;
    this.api.newToken(this.detail.id).subscribe({
      next: r => { this.busy = false; this.testMessage = { ok: r.ok, text: r.message }; this.refreshDetail(); },
      error: () => (this.busy = false),
    });
  }

  testAll(): void {
    this.api.testAll().subscribe(results => {
      const bad = results.filter(r => !r.ok);
      if (!bad.length) this.message.success('Every switched-on courier is working.');
      else this.message.warning(bad.map(r => r.message).join('  •  '), { nzDuration: 8000 });
      this.refreshDetail();
    });
  }

  disconnect(): void {
    if (!this.detail) return;
    this.api.disconnect(this.detail.id).subscribe(d => {
      this.applyDetail(d);
      this.revealed = {};
      this.loadOverview();
      this.message.success('All keys removed.');
    });
  }

  loadStores(): void {
    if (!this.detail) return;
    this.storesLoading = true;
    this.api.getPathaoStores(this.detail.id).subscribe({
      next: s => { this.stores = s; this.storesLoading = false; this.refreshDetail(); },
      error: () => (this.storesLoading = false),
    });
  }

  saveSettings(): void {
    if (!this.detail) return;
    const store = this.stores.find(s => s.storeId === this.settings.pickupStoreId);
    this.busy = true;
    this.api.updateSettings(this.detail.id, {
      displayName: this.settings.displayName, shortCode: this.settings.shortCode, color: this.settings.color, isEnabled: this.settings.isEnabled,
      pickupStoreId: this.settings.pickupStoreId, pickupStoreName: store?.storeName ?? this.detail.pickupStoreName ?? null,
      defaultDeliveryType: this.settings.deliveryType, defaultItemType: this.settings.itemType, defaultWeightKg: this.settings.weight,
    }).subscribe({
      next: d => { this.busy = false; this.applyDetail(d); this.loadOverview(); this.message.success('Saved.'); },
      error: () => (this.busy = false),
    });
  }

  get webhookUrl(): string {
    return this.detail?.webhookPath ? environment.apis['default'].url + this.detail.webhookPath : '';
  }

  tokenText(): string {
    const d = this.detail;
    if (!d?.tokenExpiresAt) return 'No token yet — it is issued the first time the app talks to Pathao (or press Test connection).';
    const ms = new Date(d.tokenExpiresAt).getTime() - Date.now();
    if (ms <= 0) return 'The saved token has expired; a new one is fetched on the next call.';
    const days = Math.floor(ms / 86400000);
    const hours = Math.floor((ms % 86400000) / 3600000);
    return `Token refreshes on its own. Current one expires in ${days ? days + ' days ' : ''}${hours} hours.`;
  }

  logEndpoint(l: CourierLogDto): string {
    return [l.method, l.endpoint].filter(Boolean).join(' ');
  }

  addCourier(): void {
    const m = this.addModel;
    if (!m.displayName.trim() || !m.shortCode.trim()) return void this.message.warning('Give the courier a name and a short code.');
    this.api.createCourier({ provider: m.provider, displayName: m.displayName, shortCode: m.shortCode, color: m.color }).subscribe(d => {
      this.addOpen = false;
      this.loadOverview();
      this.select(d);
    });
  }

  // ── Shipments ───────────────────────────────────────────────────────────
  loadShipments(): void {
    this.api.getShipments(this.filter || undefined).subscribe(p => {
      this.page = p;
      this.selected = new Set([...this.selected].filter(id => p.ready.some(r => r.orderId === id)));
      if (this.sendCourierId == null) this.sendCourierId = p.ready.find(r => r.suggestedCourierId)?.suggestedCourierId ?? null;
    });
  }

  get sendableCouriers(): CourierSummaryDto[] {
    return (this.overview?.couriers ?? []).filter(c => c.isEnabled && (c.apiAvailable || c.isManual));
  }

  toggle(r: ReadyOrderDto, on: boolean): void {
    if (on) this.selected.add(r.orderId); else this.selected.delete(r.orderId);
  }

  toggleAll(on: boolean): void {
    this.selected = on ? new Set((this.page?.ready ?? []).map(r => r.orderId)) : new Set();
  }

  get allSelected(): boolean {
    return !!this.page?.ready.length && this.page.ready.every(r => this.selected.has(r.orderId));
  }

  get sendCourierName(): string {
    return this.sendableCouriers.find(c => c.id === this.sendCourierId)?.displayName ?? 'courier';
  }

  send(): void {
    if (!this.sendCourierId || !this.selected.size) return;
    this.sending = true;
    this.sendResults = [];
    this.api.sendParcels([...this.selected], this.sendCourierId).subscribe({
      next: results => {
        this.sending = false;
        this.sendResults = results;
        const ok = results.filter(r => r.ok).length;
        if (ok) this.message.success(`${ok} ${ok === 1 ? 'parcel' : 'parcels'} sent to ${this.sendCourierName}.`);
        if (ok < results.length) this.message.warning(`${results.length - ok} could not be sent — see the list.`);
        this.selected.clear();
        this.loadShipments();
        this.loadOverview();
      },
      error: () => (this.sending = false),
    });
  }

  track(s: ShipmentDto): void {
    this.refreshing.add(s.id);
    this.api.refreshShipment(s.id).subscribe({
      next: updated => {
        this.refreshing.delete(s.id);
        const list = this.page?.shipments ?? [];
        const i = list.findIndex(x => x.id === s.id);
        if (i >= 0) list[i] = updated;
        this.message.info(`${updated.consignmentId}: ${updated.status}`);
      },
      error: () => this.refreshing.delete(s.id),
    });
  }

  stagePill(stage: string): string {
    return ({ delivered: 'is-green', transit: 'is-blue', picked: 'is-blue', ready: 'is-grey', failed: 'is-red', returned: 'is-amber', cancelled: 'is-grey' } as Record<string, string>)[stage] ?? 'is-grey';
  }

  ago(iso?: string | null): string {
    if (!iso) return '';
    const mins = Math.round((Date.now() - new Date(iso).getTime()) / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins} min ago`;
    const h = Math.round(mins / 60);
    if (h < 24) return `${h} ${h === 1 ? 'hour' : 'hours'} ago`;
    const d = Math.round(h / 24);
    return d === 1 ? 'yesterday' : `${d} days ago`;
  }

  resultFor(orderId: number): SendParcelResultDto | undefined {
    return this.sendResults.find(r => r.orderId === orderId && !r.ok);
  }
}
