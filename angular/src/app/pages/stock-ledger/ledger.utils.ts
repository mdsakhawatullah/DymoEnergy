import { LedgerFlags, LedgerLineDto, LedgerMovement } from '../../proxy/stock/ledger.models';
import { fmtFull } from '../budgets-costs/finance.utils';

export const money = (v: number | null | undefined) => fmtFull(v ?? 0, '৳');

/** 4860000 → "৳48.6 lakh". */
export function lakh(v: number): string {
  if (Math.abs(v) < 100000) return money(v);
  if (Math.abs(v) >= 10000000) return '৳' + (v / 10000000).toFixed(2).replace(/\.?0+$/, '') + ' crore';
  return '৳' + (v / 100000).toFixed(1).replace(/\.0$/, '') + ' lakh';
}

export function signed(v: number): string {
  return v > 0 ? `+${v.toLocaleString('en-IN')}` : v < 0 ? `−${Math.abs(v).toLocaleString('en-IN')}` : '0';
}

/** Green for stock coming in, red for going out, blue for a move that changes no total. */
export function movementTone(m: LedgerMovement): 'green' | 'red' | 'blue' | 'amber' | 'grey' {
  switch (m) {
    case LedgerMovement.Received:
    case LedgerMovement.Opening:
      return 'green';
    case LedgerMovement.Sold:
    case LedgerMovement.UsedOnJob:
      return 'red';
    case LedgerMovement.TransferredIn:
    case LedgerMovement.TransferredOut:
      return 'blue';
    case LedgerMovement.CountCorrection:
    case LedgerMovement.Damaged:
    case LedgerMovement.Lost:
    case LedgerMovement.Reversal:
      return 'amber';
    default:
      return 'grey';
  }
}

export function isHandCorrection(m: LedgerMovement): boolean {
  return m === LedgerMovement.CountCorrection || m === LedgerMovement.Damaged || m === LedgerMovement.Lost;
}

/** The most serious flag decides the colour of the row. */
export function flagTone(flags: LedgerFlags): 'red' | 'amber' | 'grey' {
  if (flags & (LedgerFlags.SerialNotReceived | LedgerFlags.WouldGoBelowZero | LedgerFlags.NotApproved)) return 'red';
  if (flags !== LedgerFlags.None) return 'amber';
  return 'grey';
}

export function shortHash(hash: string | null | undefined): string {
  return hash ? hash.slice(0, 6) : '';
}

export function isoDate(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** Downloads rows as a CSV file, with a BOM so Excel reads Bangla and ৳ correctly. */
export function downloadCsv(name: string, head: string[], rows: (string | number | null | undefined)[][]): void {
  const csv = [head, ...rows]
    .map(r => r.map(v => `"${String(v ?? '').replace(/"/g, '""')}"`).join(','))
    .join('\r\n');
  const url = URL.createObjectURL(new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' }));
  const a = document.createElement('a');
  a.href = url;
  a.download = name;
  a.click();
  URL.revokeObjectURL(url);
}

export function lineToRow(l: LedgerLineDto): (string | number)[] {
  return [
    l.id, new Date(l.time).toISOString(), l.productName, l.sku ?? '', l.warehouseName, l.movementLabel,
    l.change, l.quantityBefore, l.quantityAfter, l.userName, l.ipAddress ?? '', l.documentNumber ?? '',
    l.flagLabels.join('; '), l.reviewed ? 'reviewed' : '',
  ];
}

export const CSV_HEAD = [
  'Line', 'When (UTC)', 'Product', 'SKU', 'Warehouse', 'What happened',
  'Change', 'Quantity before', 'Quantity after', 'Person', 'IP address', 'Document',
  'Flagged', 'Reviewed',
];
