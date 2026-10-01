import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { SharedModule } from '../../shared/shared.module';
import { QuoteRequestService } from '../../proxy/quote-requests/quote-request.service';
import {
  QuoteRequestDto,
  QuoteRequestStatusLabels,
  QuoteRequestStatusType,
  QuoteRequestSummaryDto,
} from '../../proxy/quote-requests/models';
import { QuoteRequestDetailComponent } from './detail-panel/quote-request-detail.component';
import { QuoteStatusTone, avatarTone, initials, relativeReceived } from './quote-format';

type TabKey = 'all' | 'new' | 'contacted' | 'quoted' | 'closed';

/** Below this width the detail panel becomes an overlay instead of a side column. */
const SPLIT_MIN_WIDTH = 1100;

@Component({
  selector:    'app-quote-requests',
  templateUrl: './quote-requests.component.html',
  styleUrl:    './quote-requests.component.css',
  imports:     [SharedModule, QuoteRequestDetailComponent],
})
export class QuoteRequestsComponent implements OnInit, OnDestroy {

  quoteRequests: QuoteRequestDto[] = [];
  totalCount   = 0;
  pageIndex    = 1;
  readonly pageSize = 20;
  filter       = '';
  systemType: string | null = null;
  loading      = false;
  activeTab: TabKey = 'all';

  summary: QuoteRequestSummaryDto = {
    allCount: 0, newCount: 0, contactedCount: 0, quotedCount: 0, closedCount: 0, systemTypes: [],
  };

  readonly tabs: { key: TabKey; label: string; status?: QuoteRequestStatusType; count: (s: QuoteRequestSummaryDto) => number }[] = [
    { key: 'all',       label: 'All',                   count: s => s.allCount       },
    { key: 'new',       label: 'New',       status: 1,  count: s => s.newCount       },
    { key: 'contacted', label: 'Contacted', status: 2,  count: s => s.contactedCount },
    { key: 'quoted',    label: 'Quoted',    status: 4,  count: s => s.quotedCount    },
    { key: 'closed',    label: 'Closed',    status: 3,  count: s => s.closedCount    },
  ];

  selected: QuoteRequestDto | null = null;

  readonly statusLabels = QuoteRequestStatusLabels;
  readonly statusTone   = QuoteStatusTone;
  readonly initials     = initials;
  readonly avatarTone   = avatarTone;
  readonly relative     = relativeReceived;

  private search$ = new Subject<string>();
  private subs    = new Subscription();

  constructor(private quoteRequestService: QuoteRequestService) {}

  ngOnInit(): void {
    this.subs.add(
      this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {
        this.pageIndex = 1;
        this.reload();
      }),
    );
    this.reload();
  }

  ngOnDestroy(): void { this.subs.unsubscribe(); }

  get pageCount(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }

  /** "~5 kW · Halishahar, Chattogram", falling back to the start of the message. */
  subline(q: QuoteRequestDto): string {
    const parts = [q.estimatedSize, q.location].filter(Boolean);
    return parts.length ? parts.join(' · ') : (q.message ?? '');
  }

  // ── Data ──────────────────────────────────────────────────────────────────

  reload(): void {
    this.loadSummary();
    this.loadData();
  }

  loadSummary(): void {
    this.quoteRequestService.getSummary({
      filter:   this.filter.trim() || undefined,
      interest: this.systemType ?? undefined,
    }).subscribe(s => this.summary = s);
  }

  loadData(): void {
    this.loading = true;
    this.quoteRequestService.getListData({
      filter:         this.filter.trim() || undefined,
      interest:       this.systemType ?? undefined,
      status:         this.tabs.find(t => t.key === this.activeTab)?.status,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    }).subscribe({
      next: result => {
        this.quoteRequests = result.items;
        this.totalCount    = result.totalCount;
        this.loading       = false;

        // Keep the open request in sync; on wide screens open the newest one by default
        if (this.selected) {
          this.selected = result.items.find(i => i.id === this.selected!.id) ?? this.selected;
        } else if (result.items.length && window.innerWidth >= SPLIT_MIN_WIDTH) {
          this.selected = result.items[0];
        }
      },
      error: () => { this.loading = false; },
    });
  }

  // ── Interaction ───────────────────────────────────────────────────────────

  select(q: QuoteRequestDto): void { this.selected = q; }
  closeDetail(): void              { this.selected = null; }

  selectTab(tab: TabKey): void   { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearchInput(v: string): void { this.search$.next(v); }
  onSystemTypeChange(): void     { this.pageIndex = 1; this.reload(); }
  prevPage(): void { if (this.pageIndex > 1)              { this.pageIndex--; this.loadData(); } }
  nextPage(): void { if (this.pageIndex < this.pageCount) { this.pageIndex++; this.loadData(); } }

  /** Patch the row in place so the list does not jump while working through leads. */
  onChanged(updated: QuoteRequestDto): void {
    this.quoteRequests = this.quoteRequests.map(q => q.id === updated.id ? updated : q);
    this.selected = updated;
    this.loadSummary();
  }

  onDeleted(id: number): void {
    const idx  = this.quoteRequests.findIndex(q => q.id === id);
    const next = this.quoteRequests[idx + 1] ?? this.quoteRequests[idx - 1] ?? null;
    this.selected = window.innerWidth >= SPLIT_MIN_WIDTH ? next : null;
    this.reload();
  }
}
