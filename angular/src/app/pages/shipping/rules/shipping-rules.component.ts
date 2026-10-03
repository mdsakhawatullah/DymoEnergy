import { Component, Input, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ShippingConfigService } from '../../../proxy/shipping/shipping-config.service';
import { CourierRuleDto, CreateUpdateCourierRuleDto, RulesPageDto, ShippingItemKind } from '../../../proxy/shipping/config.models';
import { tint } from '../../project-planning/project-planning.utils';
import { ShippingItemListComponent } from '../item-list/shipping-item-list.component';

/** The editable copy of a rule; empty strings and nulls mean "this condition is not used". */
interface RuleForm {
  isEnabled: boolean;
  matchAny: boolean;
  productKeyword: string;
  anyItemOverKg: number | null;
  totalWeightUnderKg: number | null;
  addressContains: string;
  codOver: number | null;
  needsInstallation: boolean;
  courierAccountId: number | null;
  noParcel: boolean;
  thenNote: string;
}

@Component({
  selector: 'app-shipping-rules',
  templateUrl: './shipping-rules.component.html',
  styleUrls: ['../shipping.component.css', './shipping-rules.component.css'],
  imports: [SharedModule, ShippingItemListComponent],
})
export class ShippingRulesComponent implements OnInit {
  @Input() canEdit = false;

  readonly Kind = ShippingItemKind;
  page: RulesPageDto | null = null;
  failed = false;
  tint = tint;

  open = false;
  saving = false;
  editId: number | null = null;
  form: RuleForm = this.blank();

  constructor(private api: ShippingConfigService, private message: NzMessageService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed = false;
    this.api.getRules().subscribe({ next: p => (this.page = p), error: () => (this.failed = true) });
  }

  /** The number staff see on the Shipments tab: position among switched-on rules. */
  liveNumber(r: CourierRuleDto): number | null {
    if (!r.isEnabled) return null;
    return (this.page?.rules ?? []).filter(x => x.isEnabled).indexOf(r) + 1;
  }

  courierName(id?: number | null): string {
    return this.page?.couriers.find(c => c.id === id)?.displayName ?? 'Courier removed';
  }

  get enabledCouriers() {
    return (this.page?.couriers ?? []).filter(c => c.isEnabled || c.id === this.form.courierAccountId);
  }

  // ── Editing ─────────────────────────────────────────────────────────────
  private blank(): RuleForm {
    return {
      isEnabled: true, matchAny: false, productKeyword: '', anyItemOverKg: null, totalWeightUnderKg: null, addressContains: '',
      codOver: null, needsInstallation: false, courierAccountId: null, noParcel: false, thenNote: '',
    };
  }

  start(r?: CourierRuleDto): void {
    this.editId = r?.id ?? null;
    this.form = r
      ? {
          isEnabled: r.isEnabled, matchAny: r.matchAny, productKeyword: r.productKeyword ?? '', anyItemOverKg: r.anyItemOverKg ?? null,
          totalWeightUnderKg: r.totalWeightUnderKg ?? null, addressContains: r.addressContains ?? '', codOver: r.codOver ?? null,
          needsInstallation: r.needsInstallation, courierAccountId: r.courierAccountId ?? null, noParcel: r.noParcel, thenNote: r.thenNote ?? '',
        }
      : { ...this.blank(), courierAccountId: this.page?.couriers.find(c => c.isEnabled)?.id ?? null };
    this.open = true;
  }

  get conditionCount(): number {
    const f = this.form;
    return [f.productKeyword.trim(), f.anyItemOverKg, f.totalWeightUnderKg, f.addressContains.trim(), f.codOver, f.needsInstallation || null]
      .filter(v => v !== '' && v != null).length;
  }

  private toDto(f: RuleForm, order: number): CreateUpdateCourierRuleDto {
    return {
      order, isEnabled: f.isEnabled, matchAny: f.matchAny, productKeyword: f.productKeyword.trim() || null, anyItemOverKg: f.anyItemOverKg,
      totalWeightUnderKg: f.totalWeightUnderKg, addressContains: f.addressContains.trim() || null, codOver: f.codOver,
      needsInstallation: f.needsInstallation, courierAccountId: f.noParcel ? null : f.courierAccountId, noParcel: f.noParcel,
      thenNote: f.thenNote.trim() || null,
    };
  }

  private fromDto(r: CourierRuleDto, order: number, patch: Partial<CreateUpdateCourierRuleDto> = {}): CreateUpdateCourierRuleDto {
    return {
      order, isEnabled: r.isEnabled, matchAny: r.matchAny, productKeyword: r.productKeyword, anyItemOverKg: r.anyItemOverKg,
      totalWeightUnderKg: r.totalWeightUnderKg, addressContains: r.addressContains, codOver: r.codOver, needsInstallation: r.needsInstallation,
      courierAccountId: r.courierAccountId, noParcel: r.noParcel, thenNote: r.thenNote, ...patch,
    };
  }

  save(): void {
    if (!this.form.noParcel && !this.form.courierAccountId) return void this.message.warning('Choose a courier, or tick “no courier parcel”.');
    const existing = this.page?.rules.find(r => r.id === this.editId);
    const dto = this.toDto(this.form, existing?.order ?? 0);
    this.saving = true;
    const call = this.editId ? this.api.updateRule(this.editId, dto) : this.api.createRule(dto);
    call.subscribe({
      next: () => { this.saving = false; this.open = false; this.load(); },
      error: () => (this.saving = false),
    });
  }

  toggle(r: CourierRuleDto, on: boolean): void {
    this.api.updateRule(r.id, this.fromDto(r, r.order, { isEnabled: on })).subscribe(() => this.load());
  }

  remove(r: CourierRuleDto): void {
    this.api.deleteRule(r.id).subscribe(() => { this.message.success('Rule removed.'); this.load(); });
  }

  move(index: number, step: -1 | 1): void {
    const rules = this.page?.rules ?? [];
    const a = rules[index], b = rules[index + step];
    if (!a || !b) return;
    const orderA = a.order === b.order ? index + 1 : a.order;
    const orderB = a.order === b.order ? index + 1 + step : b.order;
    this.api.updateRule(a.id, this.fromDto(a, orderB)).subscribe(() => this.api.updateRule(b.id, this.fromDto(b, orderA)).subscribe(() => this.load()));
  }

  successClass(v?: number | null): string {
    if (v == null) return 'is-grey';
    return v >= 95 ? 'is-green' : v >= 85 ? 'is-amber' : 'is-red';
  }
}
