/** "2h ago", "Yesterday", "22 Sep" (or "22 Sep 2025" for older years). */
export function relativeReceived(iso: string, now = new Date()): string {
  const d    = new Date(iso);
  const mins = Math.floor((now.getTime() - d.getTime()) / 60_000);
  if (mins < 1)  return 'Just now';
  if (mins < 60) return `${mins}m ago`;

  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  if (d.getTime() >= startOfToday) return `${Math.floor(mins / 60)}h ago`;
  if (d.getTime() >= startOfToday - 86_400_000) return 'Yesterday';

  return d.toLocaleDateString('en-GB', {
    day: 'numeric', month: 'short',
    ...(d.getFullYear() !== now.getFullYear() ? { year: 'numeric' } : {}),
  });
}

/** "today, 10:42 am" / "yesterday, 4:05 pm" / "22 Sep, 9:10 am". */
export function receivedStamp(iso: string, now = new Date()): string {
  const d    = new Date(iso);
  const time = d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }).toLowerCase();
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  if (d.getTime() >= startOfToday) return `today, ${time}`;
  if (d.getTime() >= startOfToday - 86_400_000) return `yesterday, ${time}`;
  return `${d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })}, ${time}`;
}

export function initials(name?: string | null): string {
  return (name ?? '').split(/\s+/).filter(Boolean).map(w => w[0].toUpperCase()).slice(0, 2).join('') || '?';
}

/** Avatar tint picked from the name so each lead keeps the same colour. */
export function avatarTone(name?: string | null): number {
  let h = 0;
  for (const c of name ?? '') h = (h * 31 + c.charCodeAt(0)) >>> 0;
  return h % 5;
}

/** Tone per status: New amber · Contacted blue · Quoted green · Closed gray. */
export const QuoteStatusTone: Record<number, 'amber' | 'blue' | 'green' | 'gray'> = {
  1: 'amber', 2: 'blue', 4: 'green', 3: 'gray',
};

/** Local BD mobile (01XXXXXXXXX) → 8801XXXXXXXXX for wa.me / tel links. */
export function intlPhone(phone?: string | null): string {
  let d = (phone ?? '').replace(/\D/g, '');
  if (d.length === 11 && d.startsWith('0')) d = '88' + d;
  return d;
}
