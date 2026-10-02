/** Preset swatches offered next to every colour picker. */
export const COLOR_SWATCHES = ['#0E6B3F', '#2563EB', '#D97706', '#B42318', '#7C3AED', '#0F766E', '#DB2777', '#6B7280'];

const TAG_PALETTE: { bg: string; fg: string }[] = [
  { bg: '#F0EAFE', fg: '#5B21B6' },
  { bg: '#FDF0D9', fg: '#8A4B00' },
  { bg: '#E6F2EA', fg: '#0B5A34' },
  { bg: '#E8EEFC', fg: '#1E40AF' },
  { bg: '#E0F2F1', fg: '#0F5F58' },
];

/** Stable colour for a free-text tag so the same tag always looks the same. */
export function tagStyle(tag: string): { background: string; color: string } {
  let hash = 0;
  for (let i = 0; i < tag.length; i++) hash = (hash * 31 + tag.charCodeAt(i)) >>> 0;
  const p = TAG_PALETTE[hash % TAG_PALETTE.length];
  return { background: p.bg, color: p.fg };
}

/** "#RRGGBB" → rgba() with the given alpha; falls back to grey for malformed input. */
export function tint(hex: string | undefined | null, alpha: number): string {
  const m = /^#?([0-9a-f]{6})$/i.exec((hex ?? '').trim());
  if (!m) return `rgba(107, 114, 128, ${alpha})`;
  const n = parseInt(m[1], 16);
  return `rgba(${(n >> 16) & 255}, ${(n >> 8) & 255}, ${n & 255}, ${alpha})`;
}

/** Compact money: 5780000 → "৳57.8L", 45000 → "৳45k". */
export function formatMoney(value: number, symbol: string): string {
  if (!value) return `${symbol}0`;
  if (value >= 100000) return `${symbol}${(value / 100000).toFixed(1).replace(/\.0$/, '')}L`;
  if (value >= 1000) return `${symbol}${Math.round(value / 1000)}k`;
  return `${symbol}${Math.round(value)}`;
}

/** "2026-10-03T00:00:00" → "2026-10-03". */
export function dateKey(value: string | Date | null | undefined): string {
  if (!value) return '';
  if (value instanceof Date) return toDateKey(value);
  return value.substring(0, 10);
}

export function toDateKey(d: Date): string {
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
}

export function parseDateKey(key: string): Date {
  const [y, m, d] = key.split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function addDays(key: string, days: number): string {
  const d = parseDateKey(key);
  d.setDate(d.getDate() + days);
  return toDateKey(d);
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

/** "3 Oct" */
export function shortDate(key: string | null | undefined): string {
  if (!key) return '';
  const d = parseDateKey(dateKey(key));
  return `${d.getDate()} ${MONTHS[d.getMonth()]}`;
}

export function weekdayName(key: string): string {
  return DAYS[parseDateKey(key).getDay()];
}

/** "3 – 8 October 2026" style range. */
export function formatRange(startKey: string, endKey: string): string {
  const s = parseDateKey(startKey);
  const e = parseDateKey(endKey);
  const longMonth = e.toLocaleString('en-GB', { month: 'long' });
  return s.getMonth() === e.getMonth()
    ? `${s.getDate()} – ${e.getDate()} ${longMonth} ${e.getFullYear()}`
    : `${shortDate(startKey)} – ${shortDate(endKey)} ${e.getFullYear()}`;
}
