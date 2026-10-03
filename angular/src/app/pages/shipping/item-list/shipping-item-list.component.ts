import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ShippingConfigService } from '../../../proxy/shipping/shipping-config.service';
import { ShippingItemDto, ShippingItemKind } from '../../../proxy/shipping/config.models';
import { tint } from '../../project-planning/project-planning.utils';

/** One editable list on the shipping page: big items, return policies, packing rules or customer messages. */
@Component({
  selector: 'app-shipping-item-list',
  templateUrl: './shipping-item-list.component.html',
  styleUrls: ['../shipping.component.css', './shipping-item-list.component.css'],
  imports: [SharedModule],
})
export class ShippingItemListComponent {
  @Input({ required: true }) kind!: ShippingItemKind;
  @Input({ required: true }) heading = '';
  @Input() sub = '';
  @Input() items: ShippingItemDto[] = [];
  @Input() canEdit = false;
  @Input() titleLabel = 'Title';
  @Input() detailLabel = 'Details';
  @Input() extraLabel = 'Tag';
  @Input() addLabel = '+ Add';
  @Output() changed = new EventEmitter<void>();

  readonly palette = ['#0E6B3F', '#2563EB', '#1E40AF', '#D97706', '#B42318', '#5F6B63', '#7C3AED'];
  tint = tint;

  open = false;
  saving = false;
  editId: number | null = null;
  model = { title: '', detail: '', extra: '', color: '#0E6B3F' };

  constructor(private api: ShippingConfigService, private message: NzMessageService) {}

  start(item?: ShippingItemDto): void {
    this.editId = item?.id ?? null;
    this.model = { title: item?.title ?? '', detail: item?.detail ?? '', extra: item?.extra ?? '', color: item?.color ?? '#0E6B3F' };
    this.open = true;
  }

  save(): void {
    if (!this.model.title.trim()) return void this.message.warning(`${this.titleLabel} is needed.`);
    const item = this.items.find(i => i.id === this.editId);
    const input = {
      kind: this.kind, title: this.model.title.trim(), detail: this.model.detail.trim() || null, extra: this.model.extra.trim() || null,
      color: this.model.color, flag: item?.flag ?? true, order: item?.order ?? 0,
    };
    this.saving = true;
    const call = this.editId ? this.api.updateItem(this.editId, input) : this.api.createItem(input);
    call.subscribe({
      next: () => { this.saving = false; this.open = false; this.changed.emit(); },
      error: () => (this.saving = false),
    });
  }

  remove(item: ShippingItemDto): void {
    this.api.deleteItem(item.id).subscribe(() => { this.message.success('Removed.'); this.changed.emit(); });
  }

  /** Swaps the position with the neighbour above or below. */
  move(index: number, step: -1 | 1): void {
    const a = this.items[index], b = this.items[index + step];
    if (!a || !b) return;
    const orderA = a.order === b.order ? index + 1 : a.order;
    const orderB = a.order === b.order ? index + 1 + step : b.order;
    const dto = (i: ShippingItemDto, order: number) => ({ kind: i.kind, title: i.title, detail: i.detail, extra: i.extra, color: i.color, flag: i.flag, order });
    this.api.updateItem(a.id, dto(a, orderB)).subscribe(() =>
      this.api.updateItem(b.id, dto(b, orderA)).subscribe(() => this.changed.emit()));
  }
}
