import { Component, Input } from '@angular/core';
import { CourierProvider } from '../../../proxy/shipping/models';
import { courierMark } from '../courier-logo';
import { tint } from '../../project-planning/project-planning.utils';

/** Small square badge for a courier: its logo when there is one, otherwise its short code on its colour. */
@Component({
  selector: 'app-courier-badge',
  template: `
    @if (mark; as m) {
      <span class="cb" [class.cb--lg]="large" [class.cb--sm]="small" [attr.title]="name"><img [src]="m" [alt]="name || 'Courier logo'" /></span>
    } @else {
      <span class="cb cb--code" [class.cb--lg]="large" [class.cb--sm]="small" [style.background]="tint(color, 0.12)" [style.color]="color" [attr.title]="name">{{ code }}</span>
    }`,
  styles: [`
    :host { display: inline-flex; flex-shrink: 0; }
    .cb { width: 38px; height: 34px; border-radius: 8px; display: inline-flex; align-items: center; justify-content: center; background: #fff; border: 1px solid var(--de-border, #E3E6E0); }
    .cb img { width: 70%; height: 70%; object-fit: contain; display: block; }
    .cb--code { border: 0; font-size: 11px; font-weight: 800; }
    .cb--lg { width: 44px; height: 44px; }
    .cb--lg.cb--code { font-size: 13px; }
    .cb--sm { width: 28px; height: 26px; border-radius: 6px; }
    .cb--sm.cb--code { font-size: 10px; }
  `],
})
export class CourierBadgeComponent {
  @Input() provider?: CourierProvider | null;
  @Input() color = '#6B7280';
  @Input() code = '';
  @Input() name = '';
  @Input() large = false;
  @Input() small = false;
  tint = tint;

  get mark(): string | null {
    return courierMark(this.provider);
  }
}
