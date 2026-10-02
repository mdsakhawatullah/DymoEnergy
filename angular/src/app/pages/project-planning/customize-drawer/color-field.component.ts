import { Component, EventEmitter, Input, Output } from '@angular/core';
import { COLOR_SWATCHES } from '../project-planning.utils';

/** Colour picker with quick swatches. Use as `<pp-color-field [(value)]="x.color" />`. */
@Component({
  selector: 'pp-color-field',
  template: `
    <div class="cf">
      <input type="color" class="cf-picker" [value]="value" (input)="pick($any($event.target).value)" [attr.aria-label]="label" />
      @for (c of swatches; track c) {
        <button type="button" class="cf-swatch" [class.is-on]="c.toLowerCase() === (value || '').toLowerCase()"
                [style.background]="c" (click)="pick(c)" [attr.aria-label]="'Use ' + c"></button>
      }
      <code class="cf-hex">{{ value }}</code>
    </div>
  `,
  styles: [`
    .cf { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .cf-picker { width: 36px; height: 32px; padding: 0; border: 1px solid #D8DDD5; border-radius: 6px; background: none; cursor: pointer; }
    .cf-swatch { width: 20px; height: 20px; border-radius: 50%; border: 2px solid #fff; box-shadow: 0 0 0 1px #D8DDD5; cursor: pointer; padding: 0; }
    .cf-swatch.is-on { box-shadow: 0 0 0 2px #16201A; }
    .cf-hex { font-size: 11px; color: #5F6B63; margin-left: 4px; }
  `],
})
export class ColorFieldComponent {
  @Input() value = '#0E6B3F';
  @Input() label = 'Colour';
  @Output() valueChange = new EventEmitter<string>();

  readonly swatches = COLOR_SWATCHES;

  pick(color: string): void {
    this.value = color;
    this.valueChange.emit(color);
  }
}
