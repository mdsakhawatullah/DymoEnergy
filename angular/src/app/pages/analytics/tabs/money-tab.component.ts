import { Component } from '@angular/core';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { SharedModule } from '../../../shared/shared.module';
import { AnalyticsFilterDto, AnalyticsMoneyDto, AnalyticsReminderDto, NamedValueDto } from '../../../proxy/analytics/models';
import { AnBarListComponent, AnChangeComponent } from '../analytics-ui';
import { formatTaka } from '../../sales-invoices/invoice-format';
import { AnalyticsTabBase } from './tab-base';

/** Status tones for lateness — always shown with the bucket label, never colour alone. */
const TONE: Record<string, string> = { green: '#0E6B3F', amber: '#D98A0B', red: '#B42318' };

@Component({
  selector:    'an-money-tab',
  templateUrl: './money-tab.component.html',
  styleUrls:   ['../analytics-shared.css', './money-tab.component.css'],
  imports:     [SharedModule, NzModalModule, AnBarListComponent, AnChangeComponent],
})
export class MoneyTabComponent extends AnalyticsTabBase<AnalyticsMoneyDto> {
  remindersOpen = false;
  /** Reminders already opened this session — so staff can work down the list. */
  sent = new Set<number>();

  protected fetch(f: AnalyticsFilterDto) { return this.api.getMoney(f); }

  get agingItems(): NamedValueDto[] {
    return (this.data?.aging ?? []).map(a => ({ name: a.name, value: a.amount, share: 0, count: a.invoices }));
  }

  get agingColors(): string[] { return (this.data?.aging ?? []).map(a => TONE[a.tone]); }

  get maxExpected(): number { return Math.max(1, ...(this.data?.expected ?? []).map(e => e.value)); }

  lateText(days?: number | null, note?: string): string {
    return days ? `${days} day${days === 1 ? '' : 's'}` : note ?? 'not due';
  }

  private message(r: AnalyticsReminderDto): string {
    const name = r.name.replace(/[\[\]]/g, '');
    return `Dear ${name}, a friendly reminder from DymoEnergy: ${formatTaka(r.due)} is due on invoice ${r.invoiceNumber} ` +
           `(${r.lateDays} day${r.lateDays === 1 ? '' : 's'} past the due date). ` +
           `Pay by bKash / Nagad with reference ${r.invoiceNumber}, or at the showroom. Thank you!`;
  }

  private digits(phone?: string): string {
    let d = (phone ?? '').replace(/\D/g, '');
    if (d.length === 11 && d.startsWith('0')) d = '88' + d;
    return d;
  }

  whatsApp(r: AnalyticsReminderDto): void {
    window.open(`https://wa.me/${this.digits(r.phone)}?text=${encodeURIComponent(this.message(r))}`, '_blank', 'noopener');
    this.sent.add(r.invoiceId);
  }

  sms(r: AnalyticsReminderDto): void {
    window.location.href = `sms:+${this.digits(r.phone)}?body=${encodeURIComponent(this.message(r))}`;
    this.sent.add(r.invoiceId);
  }
}
