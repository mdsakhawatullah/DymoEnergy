import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Observable, Subject, debounceTime, forkJoin, of, switchMap, takeUntil } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { StockService } from '../../../proxy/stock/stock.service';
import {
  OUT_REASONS,
  SaveStockEntryDto,
  StockEntryDto,
  StockEntryStatus,
  StockEntryType,
  StockOutReason,
  StockProductDto,
  StockSupplierDto,
  WarehouseDto,
} from '../../../proxy/stock/models';
import { TYPES, isoDate, money, printSlip, signed, typeMeta } from '../stock.utils';

interface Line {
  productId: number;
  name: string;
  sku?: string | null;
  image?: string | null;
  inStock: number;
  avgCost: number;
  tracksSerials: boolean;
  qty: number;
  counted: number;
  unitCost: number;
  serialsText: string;
  showSerials: boolean;
}

interface Attachment { id?: number; fileName: string; url: string; sizeBytes: number; }

const PERM = { edit: 'DymoEnergy.Stock.Edit', post: 'DymoEnergy.Stock.Post' };
const MAX_FILE = 10 * 1024 * 1024;
const NEW_SUPPLIER = -1;

@Component({
  selector: 'app-stock-entry',
  templateUrl: './stock-entry.component.html',
  styleUrls: ['../stock.css', './stock-entry.component.css'],
  imports: [SharedModule],
})
export class StockEntryComponent implements OnInit, OnDestroy {
  @ViewChild('scanInput') scanInput?: ElementRef<HTMLInputElement>;

  readonly Type = StockEntryType;
  readonly Status = StockEntryStatus;
  readonly types = TYPES;
  readonly reasons = OUT_REASONS;
  readonly NEW_SUPPLIER = NEW_SUPPLIER;
  money = money;
  signed = signed;
  typeMeta = typeMeta;

  loading = true;
  entryId: number | null = null;
  number = '';
  status: StockEntryStatus = StockEntryStatus.Draft;
  saved: StockEntryDto | null = null;

  warehouses: WarehouseDto[] = [];
  suppliers: StockSupplierDto[] = [];

  m = {
    type: StockEntryType.StockIn,
    date: new Date(),
    warehouseId: 0,
    toWarehouseId: null as number | null,
    supplierId: null as number | null,
    invoiceNumber: '',
    purchaseOrder: '',
    transportCost: 0,
    outReason: StockOutReason.Damaged as StockOutReason,
    reference: '',
    note: '',
  };
  lines: Line[] = [];
  attachments: Attachment[] = [];

  // Product scanning
  scan = '';
  suggestions: StockProductDto[] = [];
  suggestOpen = false;
  activeSuggestion = 0;
  private scan$ = new Subject<string>();

  // Quick supplier
  newSupplier: { name: string; phone: string } | null = null;

  busy: '' | 'save' | 'post' | 'reverse' = '';
  uploading = 0;
  dragOver = false;

  canEdit = false;
  canPost = false;
  private destroy$ = new Subject<void>();

  constructor(
    private api: StockService,
    private message: NzMessageService,
    private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    permissions: PermissionService,
  ) {
    this.canEdit = permissions.getGrantedPolicy(PERM.edit);
    this.canPost = permissions.getGrantedPolicy(PERM.post);
  }

  ngOnInit(): void {
    this.scan$.pipe(
      debounceTime(220),
      switchMap(text => (text.trim() ? this.api.getProducts({ filter: text.trim(), warehouseId: this.m.warehouseId || undefined, maxResultCount: 8 }) : of([]))),
      takeUntil(this.destroy$),
    ).subscribe(list => {
      this.suggestions = list;
      this.activeSuggestion = 0;
      this.suggestOpen = !!this.scan.trim();
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    const id = idParam && idParam !== 'new' ? Number(idParam) : null;
    forkJoin({ overview: this.api.getOverview(), entry: id ? this.api.get(id) : of(null), next: id ? of('') : this.api.nextNumber() })
      .subscribe({
        next: ({ overview, entry, next }) => {
          this.warehouses = overview.warehouses;
          this.suppliers = overview.suppliers;
          const def = this.activeWarehouses.find(w => w.isDefault) ?? this.activeWarehouses[0];
          this.m.warehouseId = def?.id ?? 0;
          this.m.toWarehouseId = this.activeWarehouses.find(w => w.id !== this.m.warehouseId)?.id ?? null;
          if (entry) this.fill(entry);
          else {
            this.number = next;
            this.applyQuery();
          }
          this.loading = false;
        },
        error: () => (this.loading = false),
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get readonly(): boolean {
    return this.status !== StockEntryStatus.Draft || !this.canEdit;
  }

  get activeWarehouses(): WarehouseDto[] {
    return this.warehouses.filter(w => w.isActive || w.id === this.m.warehouseId || w.id === this.m.toWarehouseId);
  }

  get activeSuppliers(): StockSupplierDto[] {
    return this.suppliers.filter(s => s.isActive || s.id === this.m.supplierId).sort((a, b) => a.name.localeCompare(b.name));
  }

  whName(id: number | null | undefined): string {
    return this.warehouses.find(w => w.id === id)?.name ?? '—';
  }

  // ── Loading an entry ────────────────────────────────────────────────────
  private fill(e: StockEntryDto): void {
    this.saved = e;
    this.entryId = e.id;
    this.number = e.number;
    this.status = e.status;
    const posted = e.status !== StockEntryStatus.Draft;
    this.m = {
      type: e.type, date: new Date(e.date), warehouseId: e.warehouseId, toWarehouseId: e.toWarehouseId ?? null, supplierId: e.supplierId ?? null,
      invoiceNumber: e.invoiceNumber ?? '', purchaseOrder: e.purchaseOrder ?? '', transportCost: e.transportCost,
      outReason: e.outReason ?? StockOutReason.Other, reference: e.reference ?? '', note: e.note ?? '',
    };
    this.lines = e.lines.map(l => ({
      productId: l.productId, name: l.productName, sku: l.sku, image: l.image,
      inStock: posted ? (l.stockBefore ?? l.inStockNow) : l.inStockNow,
      avgCost: posted ? l.landedUnitCost : (e.type === StockEntryType.StockIn ? 0 : l.lineTotal / Math.max(1, l.quantity)),
      tracksSerials: l.tracksSerials, qty: e.type === StockEntryType.Adjustment ? Math.abs(l.change) : l.quantity,
      counted: l.countedQuantity ?? 0, unitCost: l.unitCost, serialsText: l.serials.join('\n'),
      showSerials: l.serials.length > 0 || (l.tracksSerials && !posted),
    }));
    this.attachments = e.attachments.map(a => ({ ...a }));
    if (!posted) this.refreshStock();
  }

  /** ?type=in&products=1,2 from the "Receive stock" button. */
  private applyQuery(): void {
    const q = this.route.snapshot.queryParamMap;
    const t = ({ in: StockEntryType.StockIn, out: StockEntryType.StockOut, transfer: StockEntryType.Transfer, adjustment: StockEntryType.Adjustment } as Record<string, StockEntryType>)[q.get('type') ?? ''];
    if (t) this.m.type = t;
    const ids = (q.get('products') ?? '').split(',').map(Number).filter(n => n > 0);
    if (ids.length) this.api.getProducts({ ids, warehouseId: this.m.warehouseId }).subscribe(list => list.forEach(p => this.addProduct(p, 0)));
  }

  /** Re-reads "in stock" and average cost after the warehouse changes. */
  refreshStock(): void {
    if (!this.lines.length) return;
    this.api.getProducts({ ids: this.lines.map(l => l.productId), warehouseId: this.m.warehouseId }).subscribe(list => {
      for (const p of list) {
        const l = this.lines.find(x => x.productId === p.id);
        if (!l) continue;
        l.inStock = p.inStock;
        l.avgCost = p.avgCost;
        l.tracksSerials = p.tracksSerials;
      }
    });
  }

  setType(t: StockEntryType): void {
    if (this.readonly || this.m.type === t) return;
    this.m.type = t;
    if (t === StockEntryType.Adjustment) this.lines.forEach(l => (l.counted = l.inStock));
    if (t === StockEntryType.Transfer && (!this.m.toWarehouseId || this.m.toWarehouseId === this.m.warehouseId))
      this.m.toWarehouseId = this.activeWarehouses.find(w => w.id !== this.m.warehouseId)?.id ?? null;
  }

  warehouseChanged(): void {
    if (this.m.toWarehouseId === this.m.warehouseId) this.m.toWarehouseId = this.activeWarehouses.find(w => w.id !== this.m.warehouseId)?.id ?? null;
    this.refreshStock();
  }

  supplierChanged(v: number | null): void {
    if (v === NEW_SUPPLIER) {
      this.m.supplierId = null;
      this.newSupplier = { name: '', phone: '' };
    }
  }

  saveNewSupplier(): void {
    const s = this.newSupplier;
    if (!s?.name.trim()) return void this.message.warning('Give the supplier a name.');
    this.api.createSupplier({ name: s.name.trim(), phone: s.phone.trim() || null, isActive: true }).subscribe(created => {
      this.suppliers.push(created);
      this.m.supplierId = created.id;
      this.newSupplier = null;
    });
  }

  // ── Products ────────────────────────────────────────────────────────────
  onScanInput(): void {
    this.scan$.next(this.scan);
  }

  onScanKey(ev: KeyboardEvent): void {
    if (ev.key === 'ArrowDown') { ev.preventDefault(); this.activeSuggestion = Math.min(this.activeSuggestion + 1, this.suggestions.length - 1); }
    else if (ev.key === 'ArrowUp') { ev.preventDefault(); this.activeSuggestion = Math.max(this.activeSuggestion - 1, 0); }
    else if (ev.key === 'Escape') this.suggestOpen = false;
    else if (ev.key === 'Enter') { ev.preventDefault(); this.scanEnter(); }
  }

  /** Barcode scanners type the SKU and press Enter faster than the search debounce, so look it up right away. */
  private scanEnter(): void {
    const text = this.scan.trim();
    if (!text) return;
    if (this.suggestOpen && this.suggestions[this.activeSuggestion] && this.suggestions.length) {
      const exact = this.suggestions.find(p => (p.sku ?? '').toLowerCase() === text.toLowerCase());
      this.pick(exact ?? this.suggestions[this.activeSuggestion]);
      return;
    }
    this.api.getProducts({ filter: text, warehouseId: this.m.warehouseId || undefined, maxResultCount: 8 }).subscribe(list => {
      const exact = list.find(p => (p.sku ?? '').toLowerCase() === text.toLowerCase());
      if (exact || list.length === 1) this.pick(exact ?? list[0]);
      else if (!list.length) this.message.warning(`Nothing matches “${text}”.`);
      else { this.suggestions = list; this.activeSuggestion = 0; this.suggestOpen = true; }
    });
  }

  pick(p: StockProductDto): void {
    this.addProduct(p, 1);
    this.scan = '';
    this.suggestions = [];
    this.suggestOpen = false;
    setTimeout(() => this.scanInput?.nativeElement.focus());
  }

  closeSuggestions(): void {
    setTimeout(() => (this.suggestOpen = false), 150);
  }

  private addProduct(p: StockProductDto, qty: number): void {
    const existing = this.lines.find(l => l.productId === p.id);
    if (existing) {
      if (this.m.type === StockEntryType.Adjustment) existing.counted += 1; else existing.qty += Math.max(1, qty);
      this.message.info(`${p.name}: now ${this.m.type === StockEntryType.Adjustment ? existing.counted : existing.qty}.`);
      return;
    }
    this.lines.push({
      productId: p.id, name: p.name, sku: p.sku, image: p.image, inStock: p.inStock, avgCost: p.avgCost, tracksSerials: p.tracksSerials,
      qty: Math.max(1, qty), counted: p.inStock, unitCost: p.lastCost ?? p.avgCost ?? 0, serialsText: '', showSerials: p.tracksSerials,
    });
  }

  focusScan(): void {
    this.scanInput?.nativeElement.focus();
  }

  remove(l: Line): void {
    this.lines = this.lines.filter(x => x !== l);
  }

  step(l: Line, by: number): void {
    if (this.m.type === StockEntryType.Adjustment) l.counted = Math.max(0, (l.counted || 0) + by);
    else l.qty = Math.max(0, (l.qty || 0) + by);
  }

  // ── Serials ─────────────────────────────────────────────────────────────
  serials(l: Line): string[] {
    const seen = new Set<string>();
    return l.serialsText.split(/[\n,;\t]+/).map(s => s.trim()).filter(s => s && !seen.has(s.toLowerCase()) && seen.add(s.toLowerCase()));
  }

  tidySerials(l: Line): void {
    l.serialsText = this.serials(l).join('\n');
  }

  serialPct(l: Line): number {
    return l.qty ? Math.min(100, (this.serials(l).length / l.qty) * 100) : 0;
  }

  // ── Numbers ─────────────────────────────────────────────────────────────
  delta(l: Line): number {
    switch (this.m.type) {
      case StockEntryType.StockIn: return l.qty || 0;
      case StockEntryType.Adjustment: return (l.counted || 0) - l.inStock;
      default: return -(l.qty || 0);
    }
  }

  after(l: Line): number {
    return l.inStock + this.delta(l);
  }

  lineValue(l: Line): number {
    if (this.m.type === StockEntryType.StockIn) return (l.qty || 0) * (l.unitCost || 0);
    return Math.abs(this.delta(l)) * (l.avgCost || 0);
  }

  get total(): number {
    return this.lines.reduce((s, l) => s + this.lineValue(l), 0) + (this.m.type === StockEntryType.StockIn ? this.m.transportCost || 0 : 0);
  }

  get units(): number {
    if (this.m.type === StockEntryType.Transfer) return this.lines.reduce((s, l) => s + (l.qty || 0), 0);
    return this.lines.reduce((s, l) => s + this.delta(l), 0);
  }

  get unitsText(): string {
    return this.m.type === StockEntryType.Transfer ? `${this.units} moved` : signed(this.units);
  }

  get countText(): string {
    const n = this.lines.length;
    const u = this.m.type === StockEntryType.Transfer ? this.units : this.lines.reduce((s, l) => s + Math.abs(this.delta(l)), 0);
    return `${n} ${n === 1 ? 'product' : 'products'} · ${u} ${u === 1 ? 'unit' : 'units'}`;
  }

  /** What stops the entry from being posted, in plain words. */
  get problems(): string[] {
    const out: string[] = [];
    if (!this.lines.length) out.push('Add at least one product.');
    if (this.m.type === StockEntryType.Transfer && (!this.m.toWarehouseId || this.m.toWarehouseId === this.m.warehouseId)) out.push('Choose two different warehouses.');
    for (const l of this.lines) {
      if (this.m.type !== StockEntryType.Adjustment && !(l.qty > 0)) out.push(`${l.name}: quantity is 0.`);
      else if ((this.m.type === StockEntryType.StockOut || this.m.type === StockEntryType.Transfer) && l.qty > l.inStock)
        out.push(`${l.name}: only ${l.inStock} in ${this.whName(this.m.warehouseId)}.`);
      if (this.m.type !== StockEntryType.Adjustment && this.serials(l).length > l.qty) out.push(`${l.name}: more serial numbers than units.`);
    }
    if (this.m.type === StockEntryType.Adjustment && this.lines.length && this.lines.every(l => this.delta(l) === 0)) out.push('Every count matches the stock — nothing to fix.');
    return out;
  }

  get footText(): string {
    if (this.status === StockEntryStatus.Posted) return `Posted${this.saved?.postedByName ? ' by ' + this.saved.postedByName : ''}. Stock was updated.`;
    if (this.status === StockEntryStatus.Reversed) return `Reversed by ${this.saved?.reversedByNumber ?? 'a later entry'}.`;
    const p = this.problems;
    if (p.length) return p[0] + (p.length > 1 ? ` (+${p.length - 1} more)` : '');
    return `Ready to post · ${this.lines.length} ${this.lines.length === 1 ? 'product' : 'products'}, ${this.unitsText} units`;
  }

  get detailsHint(): string {
    return ({
      [StockEntryType.StockIn]: 'From the supplier invoice or challan',
      [StockEntryType.StockOut]: 'Why it left and from where',
      [StockEntryType.Transfer]: 'Where it is moving',
      [StockEntryType.Adjustment]: 'Where you counted',
    } as Record<number, string>)[this.m.type];
  }

  get detailsTitle(): string {
    return ({
      [StockEntryType.StockIn]: 'Receiving details', [StockEntryType.StockOut]: 'Stock out details',
      [StockEntryType.Transfer]: 'Transfer details', [StockEntryType.Adjustment]: 'Count details',
    } as Record<number, string>)[this.m.type];
  }

  get title(): string {
    if (!this.entryId) return 'New stock entry';
    return this.status === StockEntryStatus.Draft ? 'Draft stock entry' : 'Stock entry';
  }

  // ── Attachments ─────────────────────────────────────────────────────────
  onFiles(files: FileList | null): void {
    if (!files) return;
    for (const f of Array.from(files)) {
      if (!/\.(pdf|jpe?g|png)$/i.test(f.name)) { this.message.warning(`${f.name}: only PDF, JPG or PNG.`); continue; }
      if (f.size > MAX_FILE) { this.message.warning(`${f.name} is over 10 MB.`); continue; }
      this.uploading++;
      this.api.upload(f).subscribe({
        next: r => {
          this.uploading--;
          const a: Attachment = { fileName: r.fileName, url: r.url, sizeBytes: r.sizeBytes };
          this.attachments.push(a);
          if (this.entryId) this.api.addAttachment(this.entryId, a).subscribe(saved => (a.id = saved.id));
        },
        error: () => { this.uploading--; this.message.error(`${f.name} could not be uploaded.`); },
      });
    }
  }

  onDrop(ev: DragEvent): void {
    ev.preventDefault();
    this.dragOver = false;
    if (this.canEdit) this.onFiles(ev.dataTransfer?.files ?? null);
  }

  removeAttachment(a: Attachment): void {
    const done = () => (this.attachments = this.attachments.filter(x => x !== a));
    if (a.id) this.api.deleteAttachment(a.id).subscribe(done); else done();
  }

  size(bytes: number): string {
    return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  // ── Save / post ─────────────────────────────────────────────────────────
  private dto(): SaveStockEntryDto {
    const t = (s: string) => s.trim() || null;
    return {
      type: this.m.type, date: isoDate(this.m.date ?? new Date()), warehouseId: this.m.warehouseId,
      toWarehouseId: this.m.type === StockEntryType.Transfer ? this.m.toWarehouseId : null,
      supplierId: this.m.type === StockEntryType.StockIn ? this.m.supplierId : null,
      invoiceNumber: t(this.m.invoiceNumber), purchaseOrder: t(this.m.purchaseOrder), transportCost: this.m.transportCost || 0,
      outReason: this.m.type === StockEntryType.StockOut ? this.m.outReason : null, reference: t(this.m.reference), note: t(this.m.note),
      lines: this.lines.map(l => ({
        productId: l.productId, quantity: l.qty || 0, countedQuantity: this.m.type === StockEntryType.Adjustment ? l.counted || 0 : null,
        unitCost: l.unitCost || 0, serials: this.m.type === StockEntryType.Adjustment ? [] : this.serials(l),
      })),
    };
  }

  /** Saves the draft and any attachments picked before it existed. */
  private save(): Observable<StockEntryDto> {
    const call = this.entryId ? this.api.update(this.entryId, this.dto()) : this.api.create(this.dto());
    return call.pipe(
      switchMap(e => {
        const firstSave = !this.entryId;
        this.entryId = e.id;
        this.number = e.number;
        // Same page, new address: a refresh or a refused post keeps working on this draft.
        if (firstSave) this.location.replaceState(`/stock-entries/${e.id}`);
        const pending = this.attachments.filter(a => !a.id);
        if (!pending.length) return of(e);
        return forkJoin(pending.map(a => this.api.addAttachment(e.id, a))).pipe(
          switchMap(saved => { saved.forEach((s, i) => (pending[i].id = s.id)); return of(e); }),
        );
      }),
    );
  }

  saveDraft(): void {
    if (!this.lines.length && !this.entryId) return void this.message.warning('Add at least one product first.');
    this.busy = 'save';
    this.save().subscribe({
      next: e => {
        this.busy = '';
        this.message.success(`Draft ${e.number} saved. Stock has not changed yet.`);
        this.router.navigate(['/stock-entries'], { queryParams: { highlight: e.id } });
      },
      error: () => (this.busy = ''),
    });
  }

  postEntry(): void {
    const p = this.problems;
    if (p.length) return void this.message.warning(p[0]);
    this.busy = 'post';
    this.save().pipe(switchMap(e => this.api.post(e.id))).subscribe({
      next: e => {
        this.busy = '';
        this.message.success(`${e.number} posted. Product stock is updated.`);
        this.router.navigate(['/stock-entries'], { queryParams: { highlight: e.id } });
      },
      // If saving worked but posting was refused, the entry is now a saved draft at its own address.
      error: () => (this.busy = ''),
    });
  }

  reverse(): void {
    if (!this.entryId) return;
    this.busy = 'reverse';
    this.api.reverse(this.entryId).subscribe({
      next: r => {
        this.busy = '';
        this.message.success(`${this.number} undone by ${r.number}.`);
        this.router.navigate(['/stock-entries'], { queryParams: { highlight: r.id } });
      },
      error: () => (this.busy = ''),
    });
  }

  print(): void {
    if (this.saved) printSlip(this.saved);
  }

  cancel(): void {
    this.router.navigate(['/stock-entries']);
  }
}
