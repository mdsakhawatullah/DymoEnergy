import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { QuoteRequestService } from '../../proxy/quote-requests/quote-request.service';
import {
  QuoteRequestDto,
  QuoteRequestStatusLabels,
  QuoteRequestStatusType,
} from '../../proxy/quote-requests/models';

type TabKey = 'all' | 'new' | 'contacted' | 'closed';

@Component({
  selector:    'app-quote-requests',
  templateUrl: './quote-requests.component.html',
  styleUrl:    './quote-requests.component.css',
  imports:     [SharedModule],
})
export class QuoteRequestsComponent implements OnInit {

  quoteRequests: QuoteRequestDto[] = [];
  totalCount = 0;
  pageIndex  = 1;
  pageSize   = 10;
  filter     = '';
  loading    = false;
  activeTab: TabKey = 'all';

  counts: Record<TabKey, number> = { all: 0, new: 0, contacted: 0, closed: 0 };

  tabs: { key: TabKey; label: string; status?: QuoteRequestStatusType }[] = [
    { key: 'all',       label: 'All' },
    { key: 'new',       label: 'New',       status: 1 },
    { key: 'contacted', label: 'Contacted', status: 2 },
    { key: 'closed',    label: 'Closed',    status: 3 },
  ];

  statusOptions: QuoteRequestStatusType[] = [1, 2, 3];
  statusLabels  = QuoteRequestStatusLabels;

  /** Dot modifier per status: New, Contacted, Closed. */
  statusDot(status: number): string {
    return { 1: 'blue', 2: 'amber', 3: '' }[status] ?? '';
  }

  deletingIds = new Set<number>();

  constructor(
    private quoteRequestService: QuoteRequestService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.loadCounts();
    this.loadData();
  }

  loadCounts(): void {
    const count = (status?: QuoteRequestStatusType) =>
      this.quoteRequestService.getListData({ status, maxResultCount: 1, skipCount: 0 });

    forkJoin({ all: count(), new: count(1), contacted: count(2), closed: count(3) }).subscribe(r => {
      this.counts = {
        all:       r.all.totalCount,
        new:       r.new.totalCount,
        contacted: r.contacted.totalCount,
        closed:    r.closed.totalCount,
      };
    });
  }

  loadData(): void {
    this.loading = true;
    this.quoteRequestService.getListData({
      filter:         this.filter || undefined,
      status:         this.tabs.find(t => t.key === this.activeTab)?.status,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    }).subscribe({
      next: result => {
        this.quoteRequests = result.items;
        this.totalCount    = result.totalCount;
        this.loading       = false;
      },
      error: () => { this.loading = false; },
    });
  }

  changeStatus(item: QuoteRequestDto, status: QuoteRequestStatusType): void {
    this.quoteRequestService.updateStatus(item.id, { status }).subscribe({
      next: () => {
        this.message.success('Status updated.');
        this.loadCounts();
        this.loadData();
      },
      error: () => this.message.error('Failed to update status.'),
    });
  }

  deleteQuoteRequest(id: number): void {
    this.deletingIds.add(id);
    this.quoteRequestService.delete(id).subscribe({
      next: () => {
        this.message.success('Quote request deleted.');
        this.deletingIds.delete(id);
        this.loadCounts();
        this.loadData();
      },
      error: () => {
        this.message.error('Failed to delete quote request.');
        this.deletingIds.delete(id);
      },
    });
  }

  selectTab(tab: TabKey): void  { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearch(): void              { this.pageIndex = 1; this.loadData(); }
  resetFilters(): void          { this.filter = ''; this.activeTab = 'all'; this.pageIndex = 1; this.loadData(); }
  onPageIndexChange(i: number)  { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)   { this.pageSize = s; this.pageIndex = 1; this.loadData(); }
}
