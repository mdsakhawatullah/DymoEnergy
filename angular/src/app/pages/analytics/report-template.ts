import { AnalyticsReportDto, AnalyticsReportKpiDto, ReportColumnType } from '../../proxy/analytics/models';
import { formatTaka } from '../sales-invoices/invoice-format';

/**
 * Renders an Analytics report as a complete A4 HTML document in the
 * "DymoEnergy report templates" design. The same document is printed for PDF
 * and shown in the on-screen viewer, so what staff see is what they save.
 */

export interface ReportBranding {
  company: string;
  logoUrl?: string | null;
  /** First header line, e.g. "Chattogram showroom · Dhaka showroom · Online". */
  line1?: string | null;
  /** Second header line, e.g. "+880 1XXX-XXXXXX · dymoenergy.com". */
  line2?: string | null;
  generatedBy: string;
}

const esc = (s: unknown) =>
  String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

const num = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 1 });

function pct(v: number): string {
  const p = v * 100;
  return `${Math.abs(p) < 10 && p % 1 !== 0 ? p.toFixed(1) : Math.round(p * 10) / 10}%`;
}

function fmtDate(v: unknown, withYear: boolean): string {
  const d = new Date(String(v));
  if (isNaN(d.getTime())) return String(v);
  return d.toLocaleDateString('en-GB', withYear ? { day: 'numeric', month: 'short', year: 'numeric' } : { day: 'numeric', month: 'short' });
}

/** Screen / PDF text for one cell. */
export function formatReportCell(v: unknown, type: ReportColumnType): string {
  if (v == null || v === '') return '—';
  switch (type) {
    case 'money':    return formatTaka(Number(v));
    case 'number':   return num.format(Number(v));
    case 'percent':
    case 'share':    return pct(Number(v));
    case 'date':     return fmtDate(v, false);
    case 'dateyear': return fmtDate(v, true);
    case 'month':    return new Date(String(v)).toLocaleDateString('en-GB', { month: 'short', year: 'numeric' });
    default:         return String(v);
  }
}

function kpiValue(k: AnalyticsReportKpiDto): string {
  if (k.value == null) return '—';
  switch (k.type) {
    case 'money':   return formatTaka(Number(k.value));
    case 'number':  return num.format(Number(k.value));
    case 'percent': return pct(Number(k.value));
    case 'date':    return fmtDate(k.value, false);
    default:        return String(k.value);
  }
}

function kpiSub(k: AnalyticsReportKpiDto): string {
  const change = k.change != null
    ? `<span class="kpi__delta ${k.change < 0 ? 'kpi__delta--down' : ''}">${k.change >= 0 ? '▲' : '▼'} ${Math.abs(k.change)}%</span> `
    : '';
  return change || k.sub ? `<div class="kpi__sub">${change}${esc(k.sub ?? '')}</div>` : '';
}

const CATEGORY_TONE: Record<string, string> = {
  'Sales': 'green', 'Products & stock': 'blue', 'Money': 'amber', 'Customers': 'purple',
};

const ALIGN_RIGHT: ReportColumnType[] = ['money', 'number', 'percent', 'share'];

export function renderReportHtml(r: AnalyticsReportDto, brand: ReportBranding, mode: 'print' | 'screen'): string {
  const now = new Date();
  const generated = `${now.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}, ${now.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}`;
  const catTone = CATEGORY_TONE[r.category] ?? 'green';

  // ── Header ────────────────────────────────────────────────────────────────
  // The official DymoEnergy app icon (assets/brand/dymo-icon.png). Absolute URL because the report
  // renders in a new window / srcdoc iframe, where relative paths do not resolve.
  const iconUrl = new URL('assets/brand/dymo-icon.png', document.baseURI).href;
  const logo = `<img class="brand__logo" src="${esc(iconUrl)}" alt="">`;
  const [first, ...rest] = brand.company.split(/(?=[A-Z][a-z]+$)/);   // "DymoEnergy" → Dymo + Energy
  const wordmark = rest.length ? `${esc(first)}<span>${esc(rest.join(''))}</span>` : esc(brand.company);

  // ── KPI tiles ─────────────────────────────────────────────────────────────
  const kpis = r.kpis.map(k => `
    <div class="kpi kpi--${k.tone}">
      <div class="kpi__label">${esc(k.label)}</div>
      <div class="kpi__value">${esc(kpiValue(k))}</div>
      ${kpiSub(k)}
    </div>`).join('');

  const steps = r.steps?.length ? `<div class="steps">${r.steps.map(k => `
    <div class="step step--${k.tone}">
      <div class="step__value">${esc(kpiValue(k))}</div>
      <div class="step__label">${esc(k.label)}</div>
    </div>`).join('')}</div>` : '';

  // ── Bars ──────────────────────────────────────────────────────────────────
  let bars = '';
  if (r.bars?.length) {
    const max = Math.max(1, ...r.bars.map(b => b.value));
    const val = (v: number) => r.barsFormat === 'money' ? formatTaka(v) : num.format(v);
    bars = `<h2 class="section">${esc(r.barsTitle ?? '')}</h2><div class="bars">${r.bars.map(b => `
      <div class="bar">
        <span class="bar__name">${esc(b.name)}</span>
        <span class="bar__track"><span class="bar__fill" style="width:${(b.value / max) * 100}%"></span></span>
        <span class="bar__value">${esc(val(b.value))}</span>
        <span class="bar__share">${pct(b.share)}</span>
      </div>`).join('')}</div>`;
  }

  // ── Table ─────────────────────────────────────────────────────────────────
  const cellClass = (i: number) => {
    const c = r.columns[i];
    return [ALIGN_RIGHT.includes(c.type) ? 'r' : '', c.bold ? 'b' : '', c.muted ? 'm' : '', c.mono ? 'mono' : ''].filter(Boolean).join(' ');
  };

  const cell = (v: unknown, i: number, tone: string | null | undefined) => {
    const c = r.columns[i];
    if (c.type === 'chip') {
      return v == null ? '<td>—</td>' : `<td><span class="chip chip--${tone ?? 'green'}">${esc(v)}</span></td>`;
    }
    if (c.type === 'share') {
      const share = Math.max(0, Math.min(1, Number(v ?? 0)));
      return `<td class="r share"><span>${esc(formatReportCell(v, 'share'))}</span><span class="mini"><span style="width:${share * 100}%"></span></span></td>`;
    }
    const toneCls = tone ? ` t-${tone}` : '';
    return `<td class="${cellClass(i)}${toneCls}">${esc(formatReportCell(v, c.type))}</td>`;
  };

  const head = r.columns.map((c, i) => `<th class="${ALIGN_RIGHT.includes(c.type) ? 'r' : ''}">${esc(c.label)}</th>`).join('');
  const body = r.rows.length
    ? r.rows.map((row, ri) => `<tr>${row.map((v, i) => cell(v, i, r.tones?.[ri]?.[i])).join('')}</tr>`).join('')
    : `<tr><td class="empty" colspan="${r.columns.length}">Nothing to report for this period.</td></tr>`;

  // Totals: label sits in the first empty cell on the left
  let foot = '';
  if (r.totals && r.rows.length) {
    let labelled = false;
    foot = `<tfoot><tr>${r.totals.map((v, i) => {
      if (v == null && !labelled && i < 3) { labelled = true; return `<td>${esc(r.totalsLabel)}</td>`; }
      if (v == null) return '<td></td>';
      return `<td class="r">${esc(formatReportCell(v, r.columns[i].type === 'share' ? 'percent' : r.columns[i].type))}</td>`;
    }).join('')}</tr></tfoot>`;
  }

  const table = `
    ${r.tableTitle ? `<h2 class="section">${esc(r.tableTitle)}</h2>` : ''}
    <table><thead><tr>${head}</tr></thead><tbody>${body}</tbody>${foot}</table>`;

  const facts = r.facts?.length ? `<div class="facts">${r.facts.map(f =>
    `<div class="fact"><strong>${esc(f.title)}</strong><div>${esc(f.text)}</div></div>`).join('')}</div>` : '';

  const note = r.note ? `<div class="note note--${r.noteTone}">${esc(r.note)}</div>` : '';

  const footerText = `${esc(brand.company)} · Internal report — do not share outside the business`;

  return `<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${esc(r.title)} — ${esc(r.reportId)}</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Instrument+Sans:wght@400;500;600;700&display=swap" rel="stylesheet">
<style>
  @page {
    size: A4; margin: 14mm 14mm 18mm;
    @bottom-left  { content: "${footerText.replace(/"/g, '\\"')}"; font: 9px "Instrument Sans", sans-serif; color: #7C877F; }
    @bottom-right { content: "Page " counter(page) " of " counter(pages); font: 9px "Instrument Sans", sans-serif; color: #7C877F; }
  }
  * { box-sizing: border-box; }
  html, body { margin: 0; background: ${mode === 'screen' ? '#EEF0EB' : '#fff'}; }
  body {
    font-family: "Instrument Sans", system-ui, sans-serif; color: #16201A; font-size: 11.5px; line-height: 1.45;
    -webkit-print-color-adjust: exact; print-color-adjust: exact;
  }
  .page { background: #fff; max-width: 794px; margin: ${mode === 'screen' ? '16px auto' : '0 auto'}; padding: ${mode === 'screen' ? '0 0 20px' : '0'};
          ${mode === 'screen' ? 'box-shadow: 0 2px 14px rgba(16,32,26,.08); border-radius: 4px; overflow: hidden;' : ''} }
  .stripe { height: 5px; background: linear-gradient(90deg, #F29D12 0 11%, #0E6B3F 11% 100%); }
  .inner { padding: ${mode === 'screen' ? '26px 40px 0' : '14px 0 0'}; }

  .top { display: flex; justify-content: space-between; align-items: flex-start; gap: 20px; }
  .brand { display: flex; align-items: center; gap: 10px; }
  .brand__mark { width: 34px; height: 34px; border-radius: 8px; background: #0E6B3F; display: inline-flex; align-items: center; justify-content: center; }
  .brand__logo { height: 36px; width: 36px; border-radius: 8px; display: block; }
  .brand__name { font-size: 19px; font-weight: 700; letter-spacing: -.01em; color: #16201A; }
  .brand__name span { color: #0E6B3F; font-weight: 600; }
  .company { text-align: right; color: #5F6B63; font-size: 10.5px; line-height: 1.6; }

  .cat { display: inline-block; margin-top: 22px; padding: 3px 9px; border-radius: 5px; font-size: 10.5px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; }
  .cat--green  { background: #E6F2EA; color: #0B5A34; }
  .cat--blue   { background: #E8EEFC; color: #1E40AF; }
  .cat--amber  { background: #FDF0D9; color: #8A4B00; }
  .cat--purple { background: #F0EAFE; color: #5B21B6; }

  .titlerow { display: flex; justify-content: space-between; align-items: flex-end; gap: 20px; margin-top: 8px; }
  h1 { font-size: 26px; font-weight: 700; letter-spacing: -.02em; margin: 0; line-height: 1.15; }
  .subtitle { font-size: 12.5px; color: #2E3A33; margin-top: 2px; }
  .meta { text-align: right; color: #5F6B63; font-size: 10.5px; line-height: 1.6; white-space: nowrap; }

  .kpis { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin: 18px 0 16px; }
  .kpi { border: 1px solid #E3E6E0; background: #F7F8F5; border-radius: 9px; padding: 11px 13px 10px; min-width: 0; }
  .kpi__label { font-size: 10.5px; font-weight: 600; color: #5F6B63; }
  .kpi__value { font-size: 19px; font-weight: 700; letter-spacing: -.01em; margin: 3px 0 2px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .kpi__sub   { font-size: 10px; color: #5F6B63; }
  .kpi__delta { font-weight: 600; color: #0B5A34; }
  .kpi__delta--down { color: #B42318; }
  .kpi--green { background: #EEF6F0; border-color: #C9E3D2; } .kpi--green .kpi__value { color: #0B5A34; }
  .kpi--amber { background: #FEF7E6; border-color: #F1DDA8; } .kpi--amber .kpi__value { color: #8A4B00; }
  .kpi--red   { background: #FDEFED; border-color: #F5C9C4; } .kpi--red   .kpi__value { color: #B42318; }

  .steps { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin: -4px 0 16px; }
  .step { border-radius: 9px; padding: 12px; text-align: center; background: #F7F8F5; }
  .step__value { font-size: 20px; font-weight: 700; }
  .step__label { font-size: 10.5px; color: #2E3A33; margin-top: 2px; }
  .step--green { background: #E6F2EA; } .step--green .step__value { color: #0B5A34; }
  .step--dark  { background: #0E6B3F; color: #fff; } .step--dark .step__label { color: #E6F2EA; }

  h2.section { font-size: 11.5px; font-weight: 700; letter-spacing: .06em; text-transform: uppercase; color: #0B5A34; margin: 14px 0 8px; }
  .bars { margin-bottom: 8px; }
  .bar { display: grid; grid-template-columns: 140px 1fr 96px 44px; gap: 12px; align-items: center; padding: 5px 0; font-size: 11.5px; }
  .bar__name  { font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .bar__track { height: 6px; border-radius: 999px; background: #EBEEE8; overflow: hidden; }
  .bar__fill  { display: block; height: 100%; border-radius: 999px; background: #0E6B3F; min-width: 3px; }
  .bar__value { text-align: right; font-variant-numeric: tabular-nums; }
  .bar__share { text-align: right; color: #5F6B63; }

  table { width: 100%; border-collapse: collapse; font-size: 11px; }
  thead { display: table-header-group; }
  tr { page-break-inside: avoid; break-inside: avoid; }
  th {
    background: #E6F2EA; color: #0B5A34; text-align: left;
    font-size: 9.5px; font-weight: 700; letter-spacing: .06em; text-transform: uppercase;
    padding: 7px 8px; white-space: nowrap;
  }
  th:first-child { border-radius: 6px 0 0 6px; } th:last-child { border-radius: 0 6px 6px 0; }
  td { padding: 7px 8px; border-bottom: 1px solid #ECEEE9; vertical-align: top; }
  tbody tr:nth-child(even) td { background: #FAFBF9; }
  .r { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
  .b { font-weight: 700; }
  .m { color: #5F6B63; }
  .mono { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 10.2px; }
  .t-green { color: #0B5A34; font-weight: 600; } .t-amber { color: #A15C00; font-weight: 600; }
  .t-red { color: #B42318; font-weight: 600; } .t-blue { color: #1E40AF; } .t-purple { color: #5B21B6; } .t-muted { color: #7C877F; }
  .chip { display: inline-block; font-size: 9.5px; font-weight: 700; padding: 1px 7px; border-radius: 4px; white-space: nowrap; }
  .chip--green  { background: #E6F2EA; color: #0B5A34; } .chip--amber { background: #FDF0D9; color: #8A4B00; }
  .chip--red    { background: #FDECEA; color: #B42318; } .chip--blue  { background: #E8EEFC; color: #1E40AF; }
  .chip--purple { background: #F0EAFE; color: #5B21B6; } .chip--muted { background: #F1F3EE; color: #5F6B63; }
  td.share { display: flex; align-items: center; justify-content: flex-end; gap: 8px; }
  .mini { display: inline-block; width: 72px; height: 5px; border-radius: 999px; background: #EBEEE8; overflow: hidden; }
  .mini span { display: block; height: 100%; background: #0E6B3F; border-radius: 999px; }
  tfoot td { font-weight: 700; border-top: 1.5px solid #16201A; border-bottom: none; padding-top: 9px; background: none !important; }
  .empty { text-align: center; color: #7C877F; padding: 26px; }

  .facts { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-top: 14px; }
  .fact { background: #F7F8F5; border-radius: 9px; padding: 11px 14px; font-size: 11px; color: #2E3A33; }
  .fact strong { display: block; margin-bottom: 2px; color: #16201A; }
  .note { margin-top: 14px; padding: 11px 14px; border-radius: 9px; font-size: 11px; background: #F4F6F2; color: #2E3A33; border: 1px solid transparent; }
  .note--amber { background: #FEF8E7; border-color: #F1DDA8; }

  .foot { display: ${mode === 'screen' ? 'flex' : 'none'}; justify-content: space-between; align-items: center;
          margin: 28px 40px 0; padding-top: 10px; border-top: 1px solid #E3E6E0; font-size: 10px; color: #7C877F; }
  @media print { .foot { display: none; } }
</style></head>
<body><div class="page">
  <div class="stripe"></div>
  <div class="inner">
    <div class="top">
      <div class="brand">${logo}<span class="brand__name">${wordmark}</span></div>
      <div class="company">${brand.line1 ? esc(brand.line1) + '<br>' : ''}${esc(brand.line2 ?? '')}</div>
    </div>

    <span class="cat cat--${catTone}">${esc(r.category)} report</span>
    <div class="titlerow">
      <div><h1>${esc(r.title)}</h1><div class="subtitle">${esc(r.subtitle)}</div></div>
      <div class="meta">Generated ${esc(generated)}<br>by ${esc(brand.generatedBy)} · Report ID ${esc(r.reportId)}</div>
    </div>

    <div class="kpis">${kpis}</div>
    ${steps}
    ${bars}
    ${table}
    ${facts}
    ${note}
  </div>
  <div class="foot"><span>${footerText}</span><span>${r.rows.length} row${r.rows.length === 1 ? '' : 's'}</span></div>
</div></body></html>`;
}
