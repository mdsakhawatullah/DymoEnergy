import { Injectable, computed, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ConfigStateService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay } from 'rxjs/operators';
import { AnalyticsService } from '../../proxy/analytics/analytics.service';
import { AnalyticsFilterDto, AnalyticsReportDto, ReportColumnType } from '../../proxy/analytics/models';
import { AdminSiteSettingService } from '../../proxy/admin-site-settings/admin-site-setting.service';
import { ReportBranding, formatReportCell, renderReportHtml } from './report-template';

/** Screen/PDF text for a report cell (kept for callers outside the template). */
export const formatCell = formatReportCell;

/**
 * Runs Analytics reports: the branded on-screen viewer, CSV download for Excel,
 * and the branded A4 print page for "Save as PDF". Provided by the Analytics page.
 */
@Injectable()
export class ReportRunnerService {
  /** Report shown in the viewer modal; null = closed. */
  readonly viewing = signal<AnalyticsReportDto | null>(null);
  readonly busyKey = signal<string | null>(null);

  private readonly brand = signal<ReportBranding>({ company: 'DymoEnergy', generatedBy: 'Admin' });
  private readonly brand$: Observable<ReportBranding>;

  /** The viewer shows exactly the page that will be printed. */
  readonly viewerHtml = computed<SafeHtml | null>(() => {
    const r = this.viewing();
    return r ? this.sanitizer.bypassSecurityTrustHtml(renderReportHtml(r, this.brand(), 'screen')) : null;
  });

  constructor(
    private analytics: AnalyticsService,
    private siteSettings: AdminSiteSettingService,
    private config: ConfigStateService,
    private sanitizer: DomSanitizer,
    private message: NzMessageService,
  ) {
    const user = this.config.getOne('currentUser');
    const by   = user?.name ? [user.name, user.surName].filter(Boolean).join(' ') : user?.userName || 'Admin';

    this.brand$ = this.siteSettings.getActive().pipe(
      catchError(() => of(null)),
      map(s => ({
        company:     s?.siteName || 'DymoEnergy',
        logoUrl:     s?.logoUrl || null,
        line1:       [s?.address, s?.city].filter(Boolean).join(', ') || null,
        line2:       [s?.phone, s?.email].filter(Boolean).join(' · ') || null,
        generatedBy: by,
      })),
      shareReplay(1),
    );
    this.brand$.subscribe(b => this.brand.set(b));
  }

  view(key: string, filter: AnalyticsFilterDto): void {
    this.fetch(key, filter, r => this.viewing.set(r));
  }

  close(): void { this.viewing.set(null); }

  csv(key: string, filter: AnalyticsFilterDto): void {
    this.fetch(key, filter, r => this.downloadCsv(r));
  }

  pdf(key: string, filter: AnalyticsFilterDto): void {
    // Open synchronously inside the click so pop-up blockers allow it, fill once data arrives
    const win = this.openWindow();
    if (!win) return;
    this.fetch(key, filter, r => this.printInto(win, r), () => win.close());
  }

  /** PDF of a report already on screen (viewer footer button). */
  pdfOf(r: AnalyticsReportDto): void {
    const win = this.openWindow();
    if (win) this.printInto(win, r);
  }

  downloadCsv(r: AnalyticsReportDto): void {
    const esc  = (v: unknown) => `"${String(v ?? '').replace(/"/g, '""')}"`;
    const cell = (v: unknown, t: ReportColumnType) =>
      v == null ? '' : (t === 'date' || t === 'dateyear') ? String(v).slice(0, 10) : v;
    const lines = [
      r.columns.map(c => esc(c.label)).join(','),
      ...r.rows.map(row => row.map((v, i) => esc(cell(v, r.columns[i].type))).join(',')),
    ];
    if (r.totals) lines.push(r.totals.map((v, i) => esc(i === 0 && v == null ? r.totalsLabel : cell(v, r.columns[i].type))).join(','));

    // BOM so Excel reads ৳ and Bangla names as UTF-8
    const blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `${r.reportId || r.key}.csv`;
    a.click();
    URL.revokeObjectURL(a.href);
  }

  private openWindow(): Window | null {
    const win = window.open('', '_blank');
    if (!win) { this.message.warning('Allow pop-ups for this site to save PDFs.'); return null; }
    win.document.write('<p style="font-family:sans-serif;padding:24px;color:#5F6B63">Preparing report…</p>');
    return win;
  }

  private printInto(win: Window, r: AnalyticsReportDto): void {
    // Wait for branding (logo, company lines) so the PDF header is complete
    this.brand$.subscribe(brand => {
      win.document.open();
      win.document.write(renderReportHtml(r, brand, 'print'));
      win.document.close();
      // Give the web font a moment, then open the print dialog ("Save as PDF")
      const go = () => setTimeout(() => { win.focus(); win.print(); }, 150);
      const fonts = (win.document as Document & { fonts?: FontFaceSet }).fonts;
      fonts?.ready ? fonts.ready.then(go) : setTimeout(go, 400);
    });
  }

  private fetch(key: string, filter: AnalyticsFilterDto, done: (r: AnalyticsReportDto) => void, failed?: () => void): void {
    this.busyKey.set(key);
    this.analytics.getReport(key, filter).subscribe({
      next: r => { this.busyKey.set(null); done(r); },
      error: () => { this.busyKey.set(null); failed?.(); },
    });
  }
}
