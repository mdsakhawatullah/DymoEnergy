import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { StockLedgerService } from '../../proxy/stock/stock-ledger.service';
import { LedgerCheckResultDto, LedgerHeaderDto } from '../../proxy/stock/ledger.models';
import { CSV_HEAD, downloadCsv, isoDate, lineToRow, signed } from './ledger.utils';
import { LedgerMovementsComponent } from './movements/ledger-movements.component';
import { LedgerProductComponent } from './product/ledger-product.component';
import { LedgerNeedsLookComponent } from './needs-look/ledger-needs-look.component';
import { LedgerAccessComponent } from './access/ledger-access.component';
import { LedgerProofComponent } from './proof/ledger-proof.component';
import { LedgerLineDrawerComponent } from './line-drawer/ledger-line-drawer.component';

type TabKey = 'movements' | 'product' | 'needs' | 'access' | 'proof';

const PERM = { settings: 'DymoEnergy.Stock.LedgerSettings' };

@Component({
  selector: 'app-stock-ledger',
  templateUrl: './stock-ledger.component.html',
  styleUrls: ['../stock-entries/stock.css', './stock-ledger.component.css'],
  imports: [
    SharedModule, LedgerMovementsComponent, LedgerProductComponent, LedgerNeedsLookComponent,
    LedgerAccessComponent, LedgerProofComponent, LedgerLineDrawerComponent,
  ],
})
export class StockLedgerComponent implements OnInit {
  signed = signed;

  tab: TabKey = 'movements';
  header: LedgerHeaderDto | null = null;
  failed = false;
  /** True when the server refused: the account may sign in but has no ledger permission. */
  forbidden = false;

  /** Line open in the drawer. */
  lineId: number | null = null;

  verifying = false;
  exporting = false;
  checkResult: LedgerCheckResultDto | null = null;

  canEditSettings = false;

  readonly tabs: { key: TabKey; label: string; icon: string }[] = [
    { key: 'movements', label: 'All movements', icon: 'bi-list' },
    { key: 'product', label: 'One product', icon: 'bi-box-seam' },
    { key: 'needs', label: 'Needs a look', icon: 'bi-exclamation-triangle' },
    { key: 'access', label: 'Who has access', icon: 'bi-people' },
    { key: 'proof', label: 'Proof & keeping', icon: 'bi-shield-check' },
  ];

  constructor(
    private api: StockLedgerService,
    private message: NzMessageService,
    route: ActivatedRoute,
    permissions: PermissionService,
  ) {
    this.canEditSettings = permissions.getGrantedPolicy(PERM.settings);
    const line = Number(route.snapshot.queryParamMap.get('line'));
    if (line) this.lineId = line;
    const tab = route.snapshot.queryParamMap.get('tab') as TabKey | null;
    if (tab && this.tabs.some(t => t.key === tab)) this.tab = tab;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.failed = false;
    this.api.getHeader().subscribe({
      next: h => (this.header = h),
      error: e => { this.failed = true; this.forbidden = e?.status === 403; },
    });
  }

  get checkedAgo(): string {
    const at = this.header?.lastCheckAt;
    if (!at) return 'not checked yet';
    const mins = Math.round((Date.now() - new Date(at).getTime()) / 60000);
    if (mins < 1) return 'checked just now';
    if (mins < 60) return `checked ${mins} minutes ago`;
    const h = Math.round(mins / 60);
    if (h < 24) return `checked ${h} ${h === 1 ? 'hour' : 'hours'} ago`;
    return `checked ${Math.round(h / 24)} days ago`;
  }

  get altered(): boolean {
    return this.header != null && this.header.lastCheckAt != null && !this.header.lastCheckOk;
  }

  verify(): void {
    this.verifying = true;
    this.api.verifyChain().subscribe({
      next: r => {
        this.verifying = false;
        this.checkResult = r;
        if (r.ok) this.message.success(r.message);
        else this.message.error(r.message, { nzDuration: 10000 });
        this.load();
      },
      error: () => (this.verifying = false),
    });
  }

  /** Everything the current filters would show, as a CSV an auditor can open. */
  export(): void {
    this.exporting = true;
    this.api.getLines({ skipCount: 0, maxResultCount: 500 }).subscribe({
      next: p => {
        this.exporting = false;
        downloadCsv(`stock-ledger-${isoDate(new Date())}.csv`, CSV_HEAD, p.items.map(lineToRow));
        if (p.totalCount > p.items.length) this.message.info(`Exported the newest ${p.items.length} of ${p.totalCount} lines.`);
      },
      error: () => (this.exporting = false),
    });
  }

  openLine(id: number): void {
    this.lineId = id;
  }

  onReviewed(): void {
    this.load();
  }
}
