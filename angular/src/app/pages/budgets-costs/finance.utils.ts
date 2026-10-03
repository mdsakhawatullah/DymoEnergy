import { FinanceSettingDto } from '../../proxy/finance/models';

const INDIAN = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 2, minimumFractionDigits: 0 });

/** 6240000 → "৳62,40,000". Negative amounts keep the minus in front of the symbol. */
export function fmtFull(value: number | null | undefined, symbol: string): string {
  const v = value ?? 0;
  return (v < 0 ? '−' : '') + symbol + INDIAN.format(Math.abs(v));
}

/** Card style: 470000 → "৳4.7L"; amounts under a lakh stay exact so a ৳6,000 profit is not shown as ৳0.1L. */
export function fmtCompact(value: number | null | undefined, s: Pick<FinanceSettingDto, 'currencySymbol' | 'compactMoney'>): string {
  const v = value ?? 0;
  if (!s.compactMoney || Math.abs(v) < 100000) return fmtFull(v, s.currencySymbol);
  return (v < 0 ? '−' : '') + s.currencySymbol + (Math.abs(v) / 100000).toFixed(1) + 'L';
}

/** Replaces {token} placeholders. */
export function fill(template: string, vars: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (_, k) => (k in vars ? String(vars[k]) : `{${k}}`));
}

/** "+14%" / "−5%" / "" when there is nothing to compare. */
export function fmtChange(pct: number | null | undefined): string {
  if (pct == null) return '';
  return (pct > 0 ? '+' : pct < 0 ? '−' : '') + Math.abs(pct) + '%';
}

export const PAYMENT_METHODS: { name: string; value: number; label: string }[] = [
  { name: 'Cash', value: 1, label: 'Cash' },
  { name: 'Card', value: 2, label: 'Card' },
  { name: 'BankTransfer', value: 3, label: 'Bank transfer' },
  { name: 'Cheque', value: 4, label: 'Cheque' },
  { name: 'Other', value: 5, label: 'Other' },
  { name: 'BKash', value: 6, label: 'bKash' },
  { name: 'Nagad', value: 7, label: 'Nagad' },
  { name: 'Rocket', value: 8, label: 'Rocket' },
];
