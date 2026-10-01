import { Pipe, PipeTransform } from '@angular/core';

// Bangladeshi grouping (1,20,200) — en-IN uses the same lakh/crore separators.
const takaFmt = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 0 });

/** ৳1,20,200 — whole taka, lakh grouping. */
export function formatTaka(value: number | null | undefined): string {
  const n = Math.round(value ?? 0);
  return (n < 0 ? '−৳' : '৳') + takaFmt.format(Math.abs(n));
}

/** Compact KPI: ৳62.4 lakh, ৳1.2 crore, ৳45,000. */
export function formatTakaCompact(value: number | null | undefined): string {
  const n = value ?? 0;
  if (Math.abs(n) >= 1e7) return `৳${(n / 1e7).toFixed(1).replace(/\.0$/, '')} crore`;
  if (Math.abs(n) >= 1e5) return `৳${(n / 1e5).toFixed(1).replace(/\.0$/, '')} lakh`;
  return formatTaka(n);
}

/** Split a free-text serial list (comma / newline / semicolon separated). */
export function parseSerials(raw?: string | null): string[] {
  return (raw ?? '').split(/[\n,;]+/).map(s => s.trim()).filter(Boolean);
}

/** "S/N A – B" for a run of serials, or the list itself when short. */
export function describeSerials(raw?: string | null): string {
  const list = parseSerials(raw);
  if (list.length === 0) return '';
  if (list.length === 1) return `S/N ${list[0]}`;
  if (list.length === 2) return `S/N ${list[0]}, ${list[1]}`;
  // Compress runs like DYM-SP550-26-000219 … DYM-SP550-26-000226 to "…000219 – 000226"
  const first = list[0], last = list[list.length - 1];
  const lastDigits  = last.match(/(\d+)$/)?.[1];
  const firstDigits = first.match(/(\d+)$/)?.[1];
  const samePrefix  = !!lastDigits && !!firstDigits &&
    first.slice(0, -firstDigits.length) === last.slice(0, -lastDigits.length);
  return `S/N ${first} – ${samePrefix ? lastDigits : last}`;
}

export function initials(name?: string | null): string {
  return (name ?? '')
    .replace(/[\[\]]/g, '')
    .split(/\s+/)
    .filter(Boolean)
    .map(w => w[0].toUpperCase())
    .slice(0, 2)
    .join('') || '?';
}

/** Rough item-icon category from the product name — purely cosmetic. */
export function itemKind(name?: string | null): 'panel' | 'inverter' | 'battery' | 'mount' | 'service' | 'cable' | 'pump' | 'other' {
  const n = (name ?? '').toLowerCase();
  if (/panel|module|perc|mono|poly/.test(n))           return 'panel';
  if (/inverter|ips|controller|mppt/.test(n))           return 'inverter';
  if (/batter|lithium|lfp|ah\b/.test(n))                return 'battery';
  if (/install|service|labou?r|commission|survey/.test(n)) return 'service';
  if (/pump|irrigation/.test(n))                        return 'pump';
  if (/cable|wire|mc4|connector/.test(n))               return 'cable';
  if (/mount|rail|bracket|clamp|structure/.test(n))     return 'mount';
  return 'other';
}

@Pipe({ name: 'taka' })
export class TakaPipe implements PipeTransform {
  transform(value: number | null | undefined, compact = false): string {
    return compact ? formatTakaCompact(value) : formatTaka(value);
  }
}
