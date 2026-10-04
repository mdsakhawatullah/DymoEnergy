import { StockEntryDto, StockEntryStatus, StockEntryType } from '../../proxy/stock/models';
import { fmtFull } from '../budgets-costs/finance.utils';

export interface TypeMeta {
  type: StockEntryType;
  label: string;
  hint: string;
  icon: string;
  color: string;
  bg: string;
}

export const TYPES: TypeMeta[] = [
  { type: StockEntryType.StockIn, label: 'Stock in', hint: 'Goods received from a supplier', icon: 'bi-download', color: '#0E6B3F', bg: '#E6F2EA' },
  { type: StockEntryType.StockOut, label: 'Stock out', hint: 'Damaged, returned or used', icon: 'bi-upload', color: '#B42318', bg: '#FDECEA' },
  { type: StockEntryType.Transfer, label: 'Transfer', hint: 'Move between warehouses', icon: 'bi-arrow-left-right', color: '#2563EB', bg: '#E8EEFC' },
  { type: StockEntryType.Adjustment, label: 'Adjustment', hint: 'Fix stock after a count', icon: 'bi-sliders', color: '#9A5B00', bg: '#FDF0D9' },
];

export const typeMeta = (t: StockEntryType): TypeMeta => TYPES.find(x => x.type === t) ?? TYPES[0];

export const money = (v: number | null | undefined) => fmtFull(v ?? 0, '৳');

/** 4860000 → "৳48.6 lakh"; under a lakh stays exact. */
export function lakh(v: number): string {
  if (Math.abs(v) < 100000) return money(v);
  if (Math.abs(v) >= 10000000) return '৳' + (v / 10000000).toFixed(2).replace(/\.?0+$/, '') + ' crore';
  return '৳' + (v / 100000).toFixed(1).replace(/\.0$/, '') + ' lakh';
}

export function signed(units: number): string {
  return units > 0 ? `+${units.toLocaleString('en-IN')}` : units < 0 ? `−${Math.abs(units).toLocaleString('en-IN')}` : '0';
}

export function statusLabel(s: StockEntryStatus): string {
  return s === StockEntryStatus.Draft ? 'Draft' : s === StockEntryStatus.Reversed ? 'Reversed' : 'Posted';
}

/** yyyy-MM-dd in local time, for the API. */
export function isoDate(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

const esc = (s: string | null | undefined) =>
  (s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!);

/** Opens a printable goods received note (or movement slip for other types) in a new window. */
export function printSlip(e: StockEntryDto): void {
  const meta = typeMeta(e.type);
  const isIn = e.type === StockEntryType.StockIn;
  const title = isIn ? 'Goods received note' : `${meta.label} slip`;
  const date = new Date(e.date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  const facts: [string, string | null | undefined][] = isIn
    ? [['Supplier', e.supplierName], ['Supplier invoice', e.invoiceNumber], ['Purchase order', e.purchaseOrder], ['Received into', e.warehouseName], ['Received on', date]]
    : e.type === StockEntryType.Transfer
      ? [['From', e.warehouseName], ['To', e.toWarehouseName], ['Date', date], ['Reference', e.reference]]
      : [['Warehouse', e.warehouseName], ['Date', date], ['Reference', e.reference]];
  const rows = e.lines.map((l, i) => `<tr><td>${i + 1}</td><td><b>${esc(l.productName)}</b><br><small>${esc(l.sku)}</small>${l.serials.length ? `<br><small>S/N: ${esc(l.serials.join(', '))}</small>` : ''}</td>
    <td class="r">${e.type === StockEntryType.Adjustment ? signed(l.change) : l.quantity}</td><td class="r">${money(isIn ? l.unitCost : l.landedUnitCost)}</td><td class="r">${money(l.lineTotal)}</td></tr>`).join('');
  const html = `<!doctype html><html><head><meta charset="utf-8"><title>${esc(e.number)} — ${title}</title><style>
    body{font:13px/1.5 system-ui,sans-serif;color:#16201A;margin:32px}h1{font-size:20px;margin:0}table{width:100%;border-collapse:collapse;margin-top:16px}
    th,td{border-bottom:1px solid #DDE2DA;padding:8px;text-align:left;vertical-align:top}th{font-size:11px;text-transform:uppercase;color:#5F6B63}.r{text-align:right}
    .facts{display:grid;grid-template-columns:repeat(3,1fr);gap:8px 24px;margin-top:16px}.facts span{display:block;font-size:11px;color:#5F6B63}
    .sign{display:flex;gap:48px;margin-top:64px}.sign div{flex:1;border-top:1px solid #16201A;padding-top:6px;font-size:12px}small{color:#5F6B63}</style></head><body>
    <div style="display:flex;justify-content:space-between"><div><h1>${title}</h1><div>${esc(e.number)}${e.status === StockEntryStatus.Draft ? ' · DRAFT' : ''}</div></div>
    <div style="text-align:right"><b>DymoEnergy</b><br><small>${e.postedAt ? 'Posted ' + new Date(e.postedAt).toLocaleString('en-GB') + (e.postedByName ? ' by ' + esc(e.postedByName) : '') : ''}</small></div></div>
    <div class="facts">${facts.filter(f => f[1]).map(f => `<div><span>${f[0]}</span>${esc(f[1])}</div>`).join('')}</div>
    <table><thead><tr><th>#</th><th>Product</th><th class="r">Qty</th><th class="r">Unit cost</th><th class="r">Total</th></tr></thead><tbody>${rows}</tbody>
    <tfoot>${isIn && e.transportCost ? `<tr><td colspan="4" class="r">Includes transport</td><td class="r">${money(e.transportCost)}</td></tr>` : ''}
    <tr><td colspan="4" class="r"><b>Total</b></td><td class="r"><b>${money(e.total)}</b></td></tr></tfoot></table>
    ${e.note ? `<p><small>Note</small><br>${esc(e.note)}</p>` : ''}
    <div class="sign"><div>${isIn ? 'Received by' : 'Prepared by'}</div><div>Checked by</div><div>${isIn ? 'Store in-charge' : 'Approved by'}</div></div>
    <script>window.onload=()=>window.print()</script></body></html>`;
  const w = window.open('', '_blank');
  if (!w) return;
  w.document.open();
  w.document.write(html);
  w.document.close();
}
