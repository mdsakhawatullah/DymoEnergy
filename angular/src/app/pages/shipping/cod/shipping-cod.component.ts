import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ShippingConfigService } from '../../../proxy/shipping/shipping-config.service';
import { CodIssueDto, CodPageDto, CodParcelDto, CodPayoutDto } from '../../../proxy/shipping/config.models';
import { fmtCompact, fmtFull } from '../../budgets-costs/finance.utils';
import { tint } from '../../project-planning/project-planning.utils';

@Component({
  selector: 'app-shipping-cod',
  templateUrl: './shipping-cod.component.html',
  styleUrls: ['../shipping.component.css', './shipping-cod.component.css'],
  imports: [SharedModule],
})
export class ShippingCodComponent implements OnInit {
  @Input() canEdit = false;
  /** Tells the page header to refresh its numbers after a payout changes. */
  @Output() changed = new EventEmitter<void>();

  page: CodPageDto | null = null;
  failed = false;
  highlightPayoutId: number | null = null;

  // ── Record payout drawer ────────────────────────────────────────────────
  payOpen = false;
  paying = false;
  pay = { courierAccountId: 0, date: new Date(), amount: 0, reference: '', note: '', financeAccountId: null as number | null };
  picked = new Set<number>();

  money = (v: number) => fmtFull(v, '৳');
  compact = (v: number) => fmtCompact(v, { currencySymbol: '৳', compactMoney: true });
  tint = tint;
  readonly Math = Math;

  constructor(private api: ShippingConfigService, private message: NzMessageService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed = false;
    this.api.getCod().subscribe({ next: p => (this.page = p), error: () => (this.failed = true) });
  }

  courierColor(id: number): string {
    return this.page?.couriers.find(c => c.courierAccountId === id)?.color ?? '#5F6B63';
  }

  // ── Issues ──────────────────────────────────────────────────────────────
  act(i: CodIssueDto): void {
    if (i.action === 'payout' && i.courierAccountId) this.startPayout(i.courierAccountId);
    else if (i.payoutId) {
      this.highlightPayoutId = i.payoutId;
      document.getElementById('cod-payout-' + i.payoutId)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  // ── Payouts ─────────────────────────────────────────────────────────────
  startPayout(courierAccountId?: number): void {
    const p = this.page;
    if (!p) return;
    const id = courierAccountId ?? p.couriers[0]?.courierAccountId;
    if (!id) return void this.message.info('No courier is holding cash right now.');
    this.pay = { courierAccountId: id, date: new Date(), amount: 0, reference: '', note: '', financeAccountId: p.financeAccounts[0]?.id ?? null };
    this.pickAll(true);
    this.payOpen = true;
  }

  onCourierChange(): void {
    this.pickAll(true);
  }

  get courierParcels(): CodParcelDto[] {
    return (this.page?.unpaid ?? []).filter(u => u.courierAccountId === this.pay.courierAccountId);
  }

  pickAll(on: boolean): void {
    this.picked = on ? new Set(this.courierParcels.map(u => u.shipmentId)) : new Set();
    this.pay.amount = this.expected;
  }

  togglePick(u: CodParcelDto, on: boolean): void {
    const wasExpected = this.pay.amount === this.expected;
    if (on) this.picked.add(u.shipmentId); else this.picked.delete(u.shipmentId);
    if (wasExpected) this.pay.amount = this.expected;   // keep following the total until the user types their own
  }

  get expected(): number {
    return Math.round(this.courierParcels.filter(u => this.picked.has(u.shipmentId)).reduce((s, u) => s + u.expected, 0) * 100) / 100;
  }

  get difference(): number {
    return Math.round(((this.pay.amount || 0) - this.expected) * 100) / 100;
  }

  savePayout(): void {
    if (!this.picked.size) return void this.message.warning('Tick the parcels this payout covers.');
    const d = this.pay.date;
    const date = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    this.paying = true;
    this.api.createPayout({
      courierAccountId: this.pay.courierAccountId, date, amount: this.pay.amount || 0, reference: this.pay.reference.trim() || null,
      note: this.pay.note.trim() || null, shipmentIds: [...this.picked], financeAccountId: this.pay.financeAccountId,
    }).subscribe({
      next: r => {
        this.paying = false;
        this.payOpen = false;
        if (r.status === 'matched') this.message.success(`Payout recorded — it matches the ${r.parcels} parcels.`);
        else this.message.warning(`Payout recorded, ${r.status} by ${this.money(Math.abs(r.difference))}.`, { nzDuration: 6000 });
        this.highlightPayoutId = r.id;
        this.load();
        this.changed.emit();
      },
      error: () => (this.paying = false),
    });
  }

  deletePayout(p: CodPayoutDto): void {
    this.api.deletePayout(p.id).subscribe(() => {
      this.message.success(`Payout removed. Its ${p.parcels} parcels are back under “courier is holding”${p.banked ? ', and the Financials entry is gone' : ''}.`);
      this.load();
      this.changed.emit();
    });
  }

  statusClass(p: CodPayoutDto): string {
    return p.status === 'matched' ? 'is-green' : p.status === 'short' ? 'is-red' : 'is-amber';
  }

  diffText(v: number): string {
    if (Math.abs(v) < 0.5) return '—';
    return (v > 0 ? '+' : '−') + this.money(Math.abs(v));
  }
}
