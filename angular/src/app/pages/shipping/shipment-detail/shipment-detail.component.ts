import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ShippingService } from '../../../proxy/shipping/shipping.service';
import { CourierEnvironment, CourierProvider, ShipmentDetailDto, ShipmentDto, ShipmentEventDto } from '../../../proxy/shipping/models';
import { fmtFull } from '../../budgets-costs/finance.utils';
import { tint } from '../../project-planning/project-planning.utils';
import { CourierBadgeComponent } from '../courier-badge/courier-badge.component';
import { courierLogo } from '../courier-logo';

type Stage = ShipmentDto['stage'];

/** Drawer with one parcel: where it is, how it got there, and the order inside it. */
@Component({
  selector: 'app-shipment-detail',
  templateUrl: './shipment-detail.component.html',
  styleUrls: ['../shipping.component.css', './shipment-detail.component.css'],
  imports: [SharedModule, CourierBadgeComponent],
})
export class ShipmentDetailComponent implements OnChanges {
  @Input({ required: true }) shipmentId!: number;
  /** Used when the parcel does not say which kind of courier it is (older API). */
  @Input() providerFallback?: CourierProvider | null;
  /** Emits the parcel after Track so the list can show the new status. */
  @Output() changed = new EventEmitter<ShipmentDto>();
  @Output() closed = new EventEmitter<void>();

  readonly Env = CourierEnvironment;
  detail: ShipmentDetailDto | null = null;
  failed = false;
  tracking = false;
  tint = tint;
  courierLogo = courierLogo;
  money = (v: number | null | undefined) => fmtFull(v ?? 0, '৳');

  /** The happy path shown as a progress line; failures and returns branch off it. */
  readonly steps: { stage: Stage; label: string }[] = [
    { stage: 'ready', label: 'Sent' },
    { stage: 'picked', label: 'Picked up' },
    { stage: 'transit', label: 'In transit' },
    { stage: 'delivered', label: 'Delivered' },
  ];

  constructor(private api: ShippingService, private message: NzMessageService) {}

  ngOnChanges(): void {
    this.load();
  }

  load(): void {
    this.failed = false;
    this.api.getShipmentDetail(this.shipmentId).subscribe({
      next: d => (this.detail = d),
      error: () => (this.failed = true),
    });
  }

  get provider(): CourierProvider | null | undefined {
    return this.detail?.shipment.courierProvider ?? this.providerFallback;
  }

  get stage(): Stage {
    return this.detail?.shipment.stage ?? 'ready';
  }

  /** How far along the happy path the parcel got; for a failed or returned parcel, the furthest step it reached. */
  get reached(): number {
    const order: Stage[] = ['ready', 'picked', 'transit', 'delivered'];
    const seen = (this.detail?.events ?? []).map(e => order.indexOf(e.stage)).filter(i => i >= 0);
    const now = order.indexOf(this.stage);
    return Math.max(now, ...seen, 0);
  }

  get offTrack(): boolean {
    return this.stage === 'failed' || this.stage === 'returned' || this.stage === 'cancelled';
  }

  /** Newest first in the timeline. */
  get events(): ShipmentEventDto[] {
    return [...(this.detail?.events ?? [])].reverse();
  }

  sourceText(e: ShipmentEventDto): string {
    switch (e.source) {
      case 'sent': return 'when the parcel was created';
      case 'webhook': return 'update from the courier';
      case 'tracked': return 'checked with Track';
      default: return 'current status';
    }
  }

  dotClass(stage: Stage): string {
    return ({ delivered: 'is-green', transit: 'is-blue', picked: 'is-blue', failed: 'is-red', returned: 'is-amber', cancelled: 'is-grey', ready: 'is-grey' } as Record<string, string>)[stage] ?? 'is-grey';
  }

  stagePill(stage: Stage): string {
    return ({ delivered: 'is-green', transit: 'is-blue', picked: 'is-blue', ready: 'is-grey', failed: 'is-red', returned: 'is-amber', cancelled: 'is-grey' } as Record<string, string>)[stage] ?? 'is-grey';
  }

  track(): void {
    if (!this.detail) return;
    this.tracking = true;
    this.api.refreshShipment(this.detail.shipment.id).subscribe({
      next: s => {
        this.tracking = false;
        this.changed.emit(s);
        this.message.info(`${s.consignmentId}: ${s.status}`);
        this.load();
      },
      error: () => (this.tracking = false),
    });
  }

  copy(text: string | null | undefined, what: string): void {
    if (!text) return;
    navigator.clipboard.writeText(text).then(() => this.message.success(`${what} copied.`), () => this.message.error('Could not copy.'));
  }

  openInvoice(): void {
    if (this.detail?.order) window.open(`/orders/${this.detail.order.id}/invoice`, '_blank', 'noopener');
  }
}
