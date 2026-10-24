import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { timeout } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { SharedModule } from '../../shared/shared.module';
import { BrandPortfolioService } from '../../proxy/brand-portfolios/brand-portfolio.service';
import {
  BrandDownloadKind,
  BrandHeroKind,
  BrandPortfolioDto,
  BrandPortfolioListDto,
  BrandProjectConsentStatus,
  CreateUpdateBrandPortfolioDto,
} from '../../proxy/brand-portfolios/models';
import { ImageUploadService } from '../../proxy/image-upload/image-upload.service';
import { CategoryService } from '../../proxy/categories/category.service';
import { userSiteUrl } from '../../../environments/environment';

/** The repeating narrative blocks of a brand page, as they go into contentJson. */
type ContentArrayName = 'quickStats' | 'facts' | 'paragraphs' | 'reasons' | 'warrantyTerms' | 'warrantyNotes';

/** Starting copy for a brand page, so a new page opens with its shape in place. */
const CONTENT_DEFAULTS = {
  quickStats: [
    { iconKey: 'box',    value: '', label: 'products we stock' },
    { iconKey: 'cal',    value: '', label: 'distributor since' },
    { iconKey: 'users',  value: '', label: 'units sold here' },
    { iconKey: 'shield', value: '', label: 'warranty, honoured by us' },
  ],
  facts: [
    { key: 'Founded', value: '' },
    { key: 'Head office', value: '' },
    { key: 'Factory', value: '' },
    { key: 'Sold in', value: '' },
    { key: 'Our distributor code', value: '' },
  ],
  paragraphs: [{ text: '' }, { text: '' }, { text: '' }],
  reasons: [
    { title: '', text: '' },
    { title: '', text: '' },
    { title: '', text: '' },
    { title: '', text: '' },
  ],
  warrantyTerms: [
    { category: 'Panels', term: '' },
    { category: 'Inverters', term: '' },
    { category: 'Batteries', term: '' },
    { category: 'Controllers', term: '' },
  ],
  warrantyNotes: [
    { text: 'You claim through us, not the factory — bring the invoice and the serial number.' },
  ],
};

/** Which icon sits beside a quick stat or a product line. */
const ICON_CHOICES = [
  { key: 'box',    label: 'Box' },
  { key: 'cal',    label: 'Calendar' },
  { key: 'users',  label: 'People' },
  { key: 'shield', label: 'Shield' },
  { key: 'panel',  label: 'Panel' },
  { key: 'bat',    label: 'Battery' },
  { key: 'inv',    label: 'Inverter' },
  { key: 'ctl',    label: 'Controller' },
];

@Component({
  selector: 'app-brand-portfolios',
  templateUrl: './brand-portfolios.component.html',
  styleUrl: './brand-portfolios.component.css',
  imports: [SharedModule, NzCheckboxModule],
})
export class BrandPortfoliosComponent implements OnInit {
  // ── View state ────────────────────────────────────────────────────────
  /** 'list' = the brands table; 'edit' = the five-tab page editor. */
  view: 'list' | 'edit' = 'list';
  /** The editor tab: 0 Basics, 1 Story, 2 Certificates, 3 Products, 4 Photos. */
  tab = 0;

  readonly heroKinds = BrandHeroKind;
  readonly downloadKinds = BrandDownloadKind;
  readonly consentStatuses = BrandProjectConsentStatus;
  readonly iconChoices = ICON_CHOICES;

  readonly tabs = [
    { label: 'Basics & facts' },
    { label: 'Story & warranty' },
    { label: 'Certificates & files' },
    { label: 'Products & projects' },
    { label: 'Photos & video' },
  ];

  // ── List ──────────────────────────────────────────────────────────────
  brands: BrandPortfolioListDto[] = [];
  totalCount = 0;
  pageIndex = 1;
  pageSize = 20;
  search = '';
  listLoading = false;
  listFailed = false;

  // ── Editor ────────────────────────────────────────────────────────────
  form!: FormGroup;
  /** null = a page that has not been saved yet. */
  editingId: number | null = null;
  editingSlug = '';
  loading = false;
  saving = false;
  savedAt: string | null = null;

  /** 0–100 while a hero video is going up, null when nothing is uploading. */
  videoProgress: number | null = null;

  /** Catalogue categories, so a product line can be tied to one. */
  categories: { value: number; displayText: string }[] = [];

  constructor(
    private fb: FormBuilder,
    private svc: BrandPortfolioService,
    private message: NzMessageService,
    private imageUpload: ImageUploadService,
    private categoryService: CategoryService
  ) {
    this.buildForm();
  }

  ngOnInit(): void {
    this.loadList();
    this.loadCategories();
  }

  // ── Form shape ────────────────────────────────────────────────────────

  buildForm(): void {
    this.form = this.fb.group({
      // Basics
      name:             [null, [Validators.required, Validators.maxLength(128)]],
      slug:             [null, [Validators.required, Validators.pattern(/^[a-z0-9]+(-[a-z0-9]+)*$/)]],
      tagline:          [null],
      logoUrl:          [null],
      shortIntro:       [null],
      distributorSince: [null],
      brandColor:       [null],
      // Search listing
      metaTitle:        [null],
      metaDescription:  [null],
      // Story
      storyHeadline:    [null],
      // Hero
      heroKind:           [BrandHeroKind.Photo],
      heroMediaUrl:       [null],
      heroCaption:        [null],
      heroAutoplayMuted:  [true],
      heroUsedForSharing: [false],
      // Call to action
      ctaHeadline:      [null],
      ctaSubline:       [null],
      // Status
      isPublished:      [false],
      isActive:         [true],
      displayOrder:     [0],
      // The narrative blocks, saved as one JSON string
      content: this.fb.group({
        quickStats:    this.fb.array([]),
        facts:         this.fb.array([]),
        paragraphs:    this.fb.array([]),
        reasons:       this.fb.array([]),
        warrantyTerms: this.fb.array([]),
        warrantyNotes: this.fb.array([]),
      }),
      // Child tables
      certificates: this.fb.array([]),
      downloads:    this.fb.array([]),
      productLines: this.fb.array([]),
      projects:     this.fb.array([]),
      media:        this.fb.array([]),
    });
  }

  get contentGroup(): FormGroup { return this.form.get('content') as FormGroup; }

  contentArray(name: ContentArrayName): FormArray {
    return this.contentGroup.get(name) as FormArray;
  }

  childArray(name: 'certificates' | 'downloads' | 'productLines' | 'projects' | 'media'): FormArray {
    return this.form.get(name) as FormArray;
  }

  // ── List ──────────────────────────────────────────────────────────────

  loadList(): void {
    this.listLoading = true;
    this.listFailed = false;
    this.svc
      .getList({
        filter: this.search?.trim() || undefined,
        skipCount: (this.pageIndex - 1) * this.pageSize,
        maxResultCount: this.pageSize,
      })
      .pipe(timeout(15000))
      .subscribe({
        next: result => {
          this.brands = result.items ?? [];
          this.totalCount = result.totalCount ?? 0;
          this.listLoading = false;
        },
        error: err => {
          this.listLoading = false;
          this.listFailed = true;
          this.message.error(
            `Could not load the brand pages (${err?.status ?? 'no response'}). Check you are signed in and have the Brand Portfolio permission.`,
            { nzDuration: 8000 }
          );
        },
      });
  }

  onSearch(): void {
    this.pageIndex = 1;
    this.loadList();
  }

  onPageIndexChange(index: number): void {
    this.pageIndex = index;
    this.loadList();
  }

  private loadCategories(): void {
    this.categoryService.getSelectList().subscribe({
      next: rows => (this.categories = rows ?? []),
      error: () => (this.categories = []),
    });
  }

  // ── Open / close the editor ───────────────────────────────────────────

  newBrand(): void {
    this.editingId = null;
    this.editingSlug = '';
    this.savedAt = null;
    this.tab = 0;
    this.form.reset({
      heroKind: BrandHeroKind.Photo,
      heroAutoplayMuted: true,
      heroUsedForSharing: false,
      isPublished: false,
      isActive: true,
      displayOrder: this.brands.length,
    });
    this.patchContent(null);
    (['certificates', 'downloads', 'productLines', 'projects', 'media'] as const).forEach(n =>
      this.childArray(n).clear()
    );
    this.view = 'edit';
  }

  openBrand(id: number): void {
    this.loading = true;
    this.view = 'edit';
    this.tab = 0;
    this.svc
      .get(id)
      .pipe(timeout(15000))
      .subscribe({
        next: dto => {
          this.patchForm(dto);
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.view = 'list';
          this.message.error('Could not open that brand page.');
        },
      });
  }

  backToList(): void {
    this.view = 'list';
    this.loadList();
  }

  patchForm(dto: BrandPortfolioDto): void {
    this.editingId = dto.id;
    this.editingSlug = dto.slug ?? '';
    this.savedAt = dto.lastModificationTime ?? dto.creationTime ?? null;

    this.form.patchValue({
      name:               dto.name,
      slug:               dto.slug,
      tagline:            dto.tagline,
      logoUrl:            dto.logoUrl,
      shortIntro:         dto.shortIntro,
      distributorSince:   dto.distributorSince,
      brandColor:         dto.brandColor,
      metaTitle:          dto.metaTitle,
      metaDescription:    dto.metaDescription,
      storyHeadline:      dto.storyHeadline,
      heroKind:           dto.heroKind,
      heroMediaUrl:       dto.heroMediaUrl,
      heroCaption:        dto.heroCaption,
      heroAutoplayMuted:  dto.heroAutoplayMuted,
      heroUsedForSharing: dto.heroUsedForSharing,
      ctaHeadline:        dto.ctaHeadline,
      ctaSubline:         dto.ctaSubline,
      isPublished:        dto.isPublished,
      isActive:           dto.isActive,
      displayOrder:       dto.displayOrder,
    });

    this.patchContent(dto.contentJson);

    const fill = (
      name: 'certificates' | 'downloads' | 'productLines' | 'projects' | 'media',
      rows: any[],
      make: (r: any) => FormGroup
    ) => {
      const arr = this.childArray(name);
      arr.clear();
      (rows ?? []).forEach(r => arr.push(make(r)));
    };

    fill('certificates', dto.certificates, r => this.certificateGroup(r));
    fill('downloads',    dto.downloads,    r => this.downloadGroup(r));
    fill('productLines', dto.productLines, r => this.productLineGroup(r));
    fill('projects',     dto.projects,     r => this.projectGroup(r));
    fill('media',        dto.media,        r => this.mediaGroup(r));
  }

  // ── Narrative blocks ──────────────────────────────────────────────────

  private contentRow(fields: Record<string, string>): FormGroup {
    return this.fb.group(Object.fromEntries(Object.entries(fields).map(([k, v]) => [k, [v]])));
  }

  private blankContentRow(name: ContentArrayName): Record<string, string> {
    return {
      quickStats:    { iconKey: 'box', value: '', label: '' },
      facts:         { key: '', value: '' },
      paragraphs:    { text: '' },
      reasons:       { title: '', text: '' },
      warrantyTerms: { category: '', term: '' },
      warrantyNotes: { text: '' },
    }[name];
  }

  addContentRow(name: ContentArrayName): void {
    this.contentArray(name).push(this.contentRow(this.blankContentRow(name)));
  }

  removeContentRow(name: ContentArrayName, index: number): void {
    this.contentArray(name).removeAt(index);
  }

  /** Moves a paragraph (or any narrative row) one place up or down. */
  moveContentRow(name: ContentArrayName, index: number, delta: number): void {
    const arr = this.contentArray(name);
    const target = index + delta;
    if (target < 0 || target >= arr.length) return;
    const row = arr.at(index);
    arr.removeAt(index);
    arr.insert(target, row);
  }

  private patchContent(json?: string | null): void {
    let saved: any = {};
    try { saved = json ? JSON.parse(json) : {}; } catch { saved = {}; }

    const fill = (name: ContentArrayName, rows: any[], make: (r: any) => Record<string, string>) => {
      const arr = this.contentArray(name);
      arr.clear();
      rows.forEach(r => arr.push(this.contentRow(make(r))));
    };

    fill('quickStats', saved.quickStats?.length ? saved.quickStats : CONTENT_DEFAULTS.quickStats,
      r => ({ iconKey: r.iconKey ?? 'box', value: r.value ?? '', label: r.label ?? '' }));

    fill('facts', saved.facts?.length ? saved.facts : CONTENT_DEFAULTS.facts,
      r => ({ key: r.key ?? '', value: r.value ?? '' }));

    fill('paragraphs', saved.paragraphs?.length ? saved.paragraphs : CONTENT_DEFAULTS.paragraphs,
      r => ({ text: r.text ?? '' }));

    fill('reasons', saved.reasons?.length ? saved.reasons : CONTENT_DEFAULTS.reasons,
      r => ({ title: r.title ?? '', text: r.text ?? '' }));

    fill('warrantyTerms', saved.warrantyTerms?.length ? saved.warrantyTerms : CONTENT_DEFAULTS.warrantyTerms,
      r => ({ category: r.category ?? '', term: r.term ?? '' }));

    fill('warrantyNotes', saved.warrantyNotes?.length ? saved.warrantyNotes : CONTENT_DEFAULTS.warrantyNotes,
      r => ({ text: r.text ?? '' }));
  }

  /** Blank rows are dropped, so the shop only renders what was filled in. */
  private contentJson(v: any): string {
    const keep = (rows: any[], key: string) =>
      (rows ?? []).filter(r => (r[key] ?? '').toString().trim());

    return JSON.stringify({
      quickStats:    keep(v.quickStats, 'value'),
      facts:         keep(v.facts, 'value'),
      paragraphs:    keep(v.paragraphs, 'text'),
      reasons:       keep(v.reasons, 'title'),
      warrantyTerms: keep(v.warrantyTerms, 'term'),
      warrantyNotes: keep(v.warrantyNotes, 'text'),
    });
  }

  // ── Child rows ────────────────────────────────────────────────────────

  private certificateGroup(r?: any): FormGroup {
    return this.fb.group({
      id:                [r?.id ?? null],
      mark:              [r?.mark ?? null],
      covers:            [r?.covers ?? null],
      certificateNumber: [r?.certificateNumber ?? null],
      noExpiry:          [r?.noExpiry ?? false],
      expiresOn:         [r?.expiresOn ? new Date(r.expiresOn) : null],
      fileUrl:           [r?.fileUrl ?? null],
      fileSize:          [r?.fileSize ?? null],
      isVisible:         [r?.isVisible ?? true],
      displayOrder:      [r?.displayOrder ?? this.childArray('certificates').length],
    });
  }

  private downloadGroup(r?: any): FormGroup {
    return this.fb.group({
      id:           [r?.id ?? null],
      title:        [r?.title ?? null],
      kind:         [r?.kind ?? BrandDownloadKind.Pdf],
      fileUrl:      [r?.fileUrl ?? null],
      fileSize:     [r?.fileSize ?? null],
      dealersOnly:  [r?.dealersOnly ?? false],
      isActive:     [r?.isActive ?? true],
      displayOrder: [r?.displayOrder ?? this.childArray('downloads').length],
    });
  }

  private productLineGroup(r?: any): FormGroup {
    return this.fb.group({
      id:           [r?.id ?? null],
      name:         [r?.name ?? null],
      blurb:        [r?.blurb ?? null],
      categoryId:   [r?.categoryId ?? null],
      iconKey:      [r?.iconKey ?? 'box'],
      isVisible:    [r?.isVisible ?? true],
      displayOrder: [r?.displayOrder ?? this.childArray('productLines').length],
      // Read-only, worked out by the server from the linked category.
      productCount: [r?.productCount ?? 0],
      fromPrice:    [r?.fromPrice ?? null],
    });
  }

  private projectGroup(r?: any): FormGroup {
    return this.fb.group({
      id:            [r?.id ?? null],
      projectId:     [r?.projectId ?? null],
      title:         [r?.title ?? null],
      subtitle:      [r?.subtitle ?? null],
      imageUrl:      [r?.imageUrl ?? null],
      consentStatus: [r?.consentStatus ?? BrandProjectConsentStatus.WaitingOnConsent],
      displayOrder:  [r?.displayOrder ?? this.childArray('projects').length],
    });
  }

  private mediaGroup(r?: any): FormGroup {
    return this.fb.group({
      id:           [r?.id ?? null],
      imageUrl:     [r?.imageUrl ?? null],
      caption:      [r?.caption ?? null],
      altText:      [r?.altText ?? null],
      isActive:     [r?.isActive ?? true],
      displayOrder: [r?.displayOrder ?? this.childArray('media').length],
    });
  }

  addCertificate(): void { this.childArray('certificates').push(this.certificateGroup()); }
  addDownload(): void    { this.childArray('downloads').push(this.downloadGroup()); }
  addProductLine(): void { this.childArray('productLines').push(this.productLineGroup()); }
  addProject(): void     { this.childArray('projects').push(this.projectGroup()); }
  addMedia(): void       { this.childArray('media').push(this.mediaGroup()); }

  removeChild(name: 'certificates' | 'downloads' | 'productLines' | 'projects' | 'media', index: number): void {
    this.childArray(name).removeAt(index);
  }

  /** Keeps displayOrder in step with what the editor shows before the page is sent. */
  private renumber(arr: FormArray): void {
    arr.controls.forEach((c, i) => c.get('displayOrder')?.setValue(i, { emitEvent: false }));
  }

  // ── Save ──────────────────────────────────────────────────────────────

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.tab = 0;
      this.message.error('The brand needs a name and a web address before it can be saved.');
      return;
    }

    (['certificates', 'downloads', 'productLines', 'projects', 'media'] as const).forEach(n =>
      this.renumber(this.childArray(n))
    );

    this.saving = true;
    const { content, certificates, downloads, productLines, projects, media, ...rest } = this.form.value;

    const payload: CreateUpdateBrandPortfolioDto = {
      ...rest,
      contentJson: this.contentJson(content),
      certificates: (certificates ?? []).map((c: any) => ({
        ...c,
        expiresOn: c.noExpiry || !c.expiresOn ? null : new Date(c.expiresOn).toISOString(),
      })),
      downloads: downloads ?? [],
      // productCount / fromPrice are the server's to work out, so they are not sent back.
      productLines: (productLines ?? []).map(({ productCount, fromPrice, ...line }: any) => line),
      projects: projects ?? [],
      media: media ?? [],
    };

    const request$ = this.editingId
      ? this.svc.update(this.editingId, payload)
      : this.svc.create(payload);

    request$.subscribe({
      next: result => {
        this.saving = false;
        const wasNew = this.editingId === null;
        this.patchForm(result);
        this.message.success(wasNew ? 'Brand page created.' : 'Brand page saved.');
      },
      error: err => {
        this.saving = false;
        this.message.error(err?.error?.error?.message ?? 'Could not save the brand page.');
      },
    });
  }

  togglePublished(brand: BrandPortfolioListDto, isPublished: boolean): void {
    this.svc.setPublished(brand.id, isPublished).subscribe({
      next: () => {
        brand.isPublished = isPublished;
        this.message.success(isPublished ? 'The page is now public.' : 'The page is no longer public.');
      },
      error: err => this.message.error(err?.error?.error?.message ?? 'Could not change that.'),
    });
  }

  /** From the editor header, where the switch is bound to the form. */
  togglePublishedInEditor(isPublished: boolean): void {
    this.form.get('isPublished')?.setValue(isPublished);
    if (this.editingId === null) return;
    this.svc.setPublished(this.editingId, isPublished).subscribe({
      next: () => this.message.success(isPublished ? 'The page is now public.' : 'The page is no longer public.'),
      error: err => {
        this.form.get('isPublished')?.setValue(!isPublished);
        this.message.error(err?.error?.error?.message ?? 'Could not change that.');
      },
    });
  }

  deleteBrand(id: number): void {
    this.svc.delete(id).subscribe({
      next: () => {
        this.message.success('Brand page deleted.');
        this.loadList();
      },
      error: () => this.message.error('Could not delete that brand page.'),
    });
  }

  // ── Completeness ──────────────────────────────────────────────────────

  /**
   * Mirrors what the server reports for the list: the share of blocks that carry
   * something. Empty blocks are hidden on the shop rather than shown blank.
   */
  get blocks(): { label: string; filled: boolean; note: string }[] {
    const v = this.form.value;
    const c = v.content ?? {};
    const count = (rows: any[], key: string) =>
      (rows ?? []).filter(r => (r[key] ?? '').toString().trim()).length;

    const stats = count(c.quickStats, 'value');
    const facts = count(c.facts, 'value');
    const paras = count(c.paragraphs, 'text');
    const reasons = count(c.reasons, 'title');
    const warranty = count(c.warrantyTerms, 'term');
    const certs = (v.certificates ?? []).filter((r: any) => (r.mark ?? '').trim()).length;
    const lines = (v.productLines ?? []).filter((r: any) => (r.name ?? '').trim()).length;
    const files = (v.downloads ?? []).filter((r: any) => (r.title ?? '').trim()).length;
    const photos = (v.media ?? []).filter((r: any) => (r.imageUrl ?? '').trim()).length;
    const projects = (v.projects ?? []).filter((r: any) => (r.title ?? '').trim()).length;

    const txt = (s?: string) => !!(s ?? '').toString().trim();

    return [
      { label: 'Basics',            filled: txt(v.name) && txt(v.slug), note: txt(v.name) ? 'filled' : 'empty' },
      { label: 'Hero',              filled: txt(v.heroMediaUrl), note: txt(v.heroMediaUrl) ? (v.heroKind === BrandHeroKind.Video ? 'video set' : 'photo set') : 'empty — hidden' },
      { label: 'Quick stats',       filled: stats > 0, note: `${stats} of 4` },
      { label: 'Story',             filled: paras > 0, note: paras ? `${paras} paragraph${paras === 1 ? '' : 's'}` : 'empty — hidden' },
      { label: 'Why we carry them', filled: reasons > 0, note: reasons ? `${reasons} reasons` : 'empty — hidden' },
      { label: 'At a glance',       filled: facts > 0, note: facts ? `${facts} rows` : 'empty — hidden' },
      { label: 'Warranty',          filled: warranty > 0, note: warranty ? `${warranty} of 4` : 'empty — hidden' },
      { label: 'Certificates',      filled: certs > 0, note: certs ? `${certs} listed` : 'empty — hidden' },
      { label: 'Downloads',         filled: files > 0, note: files ? `${files} files` : 'empty — hidden' },
      { label: 'Product lines',     filled: lines > 0, note: lines ? `${lines} listed` : 'empty — hidden' },
      { label: 'Photos',            filled: photos > 0, note: photos ? `${photos} photos` : 'empty — hidden' },
      { label: 'Projects',          filled: projects > 0, note: projects ? `${projects} jobs` : 'empty — hidden' },
      { label: 'Call to action',    filled: txt(v.ctaHeadline), note: txt(v.ctaHeadline) ? 'filled' : 'empty — hidden' },
      { label: 'Search listing',    filled: txt(v.metaTitle), note: txt(v.metaTitle) ? 'filled' : 'empty' },
    ];
  }

  get filledCount(): number { return this.blocks.filter(b => b.filled).length; }
  get blockCount(): number  { return this.blocks.length; }
  get completeness(): number {
    return Math.round((this.filledCount / this.blockCount) * 100);
  }

  /** Jumps to the tab holding the first block that is still empty. */
  jumpToFirstEmpty(): void {
    const tabOfBlock: Record<string, number> = {
      Basics: 0, 'Quick stats': 0, 'At a glance': 0, 'Search listing': 0,
      Story: 1, 'Why we carry them': 1, Warranty: 1,
      Certificates: 2, Downloads: 2,
      'Product lines': 3, Projects: 3, 'Call to action': 3,
      Hero: 4, Photos: 4,
    };
    const first = this.blocks.find(b => !b.filled);
    if (!first) { this.message.success('Every block on this page carries something.'); return; }
    this.tab = tabOfBlock[first.label] ?? 0;
  }

  // ── Uploads ───────────────────────────────────────────────────────────

  /** Opens a picker, uploads the image and puts its URL into the given control. */
  uploadImageInto(control: AbstractControl | null, folder = 'brands'): void {
    if (!control) return;
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/jpeg,image/png,image/webp';
    input.onchange = () => {
      const file = input.files?.[0];
      if (!file) return;
      if (file.size > 2 * 1024 * 1024) { this.message.error('Pick an image under 2 MB.'); return; }
      this.imageUpload.uploadImage(file, folder).subscribe({
        next: url => { control.setValue(url); this.message.success('Image uploaded.'); },
        error: () => this.message.error('Could not upload that image.'),
      });
    };
    input.click();
  }

  /**
   * Picks an MP4 (or WebM / MOV) off the machine, sends it to Cloudinary and drops the
   * returned URL into the hero field, so a brand video need not be hosted somewhere first.
   */
  uploadVideoInto(control: AbstractControl | null, folder = 'DymoEnergy/brand-heroes'): void {
    if (!control || this.videoProgress !== null) return;
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'video/mp4,video/webm,video/quicktime';
    input.onchange = () => {
      const file = input.files?.[0];
      if (!file) return;
      if (file.size > 50 * 1024 * 1024) { this.message.error('Pick a video under 50 MB.'); return; }
      this.videoProgress = 0;
      this.imageUpload.uploadVideo(file, folder).subscribe({
        next: e => {
          this.videoProgress = e.progress;
          if (e.url) {
            control.setValue(e.url);
            this.videoProgress = null;
            this.message.success('Video uploaded.');
          }
        },
        error: () => {
          this.videoProgress = null;
          this.message.error('Could not upload that video.');
        },
      });
    };
    input.click();
  }

  /**
   * Uploads the supplier's own PDF (or a ZIP of datasheets) and fills in both the URL
   * and the size shown beside it.
   */
  uploadFileInto(row: AbstractControl | null): void {
    if (!row) return;
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'application/pdf,application/zip';
    input.onchange = () => {
      const file = input.files?.[0];
      if (!file) return;
      if (file.size > 25 * 1024 * 1024) { this.message.error('Pick a file under 25 MB.'); return; }
      this.imageUpload.uploadProductPdf(file).subscribe({
        next: url => {
          row.get('fileUrl')?.setValue(url);
          row.get('fileSize')?.setValue(this.prettySize(file.size, file.name));
          this.message.success('File uploaded.');
        },
        error: () => this.message.error('Could not upload that file.'),
      });
    };
    input.click();
  }

  private prettySize(bytes: number, name: string): string {
    const kind = name.toLowerCase().endsWith('.zip') ? 'ZIP' : 'PDF';
    const mb = bytes / (1024 * 1024);
    return mb >= 1 ? `${kind} · ${mb.toFixed(1)} MB` : `${kind} · ${Math.max(1, Math.round(bytes / 1024))} KB`;
  }

  // ── Small helpers used by the template ────────────────────────────────

  /** Turns a brand name into a usable web address when the slug is still blank. */
  suggestSlug(): void {
    const slug = this.form.get('slug');
    if (slug?.value) return;
    const name = (this.form.get('name')?.value ?? '').toString();
    const suggestion = name
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
    if (suggestion) slug?.setValue(suggestion);
  }

  onColorPickerChange(controlName: string, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.form.get(controlName)?.setValue(value, { emitEvent: false });
  }

  colorValue(controlName: string): string {
    const v = this.form.get(controlName)?.value;
    return v && /^#[0-9A-Fa-f]{6}$/.test(v) ? v : '#000000';
  }

  /** Letters shown in the square when a brand has no logo yet. */
  initials(name?: string): string {
    return (name ?? '?').trim().slice(0, 6).toUpperCase();
  }

  /** A neutral example for an "At a glance" value, chosen by the row label. */
  factHint(key?: string | null): string {
    const k = (key ?? '').toLowerCase();
    if (k.includes('found') || k.includes('since')) return 'A year, e.g. 2009';
    if (k.includes('office') || k.includes('head')) return 'City, country';
    if (k.includes('factory')) return 'City · floor area in m²';
    if (k.includes('sold') || k.includes('countr')) return 'Where it is sold';
    if (k.includes('code')) return 'Your distributor code';
    return 'Value';
  }

  /**
   * Where this brand's page lives on the customer-facing shop, or null when the
   * shop's address is not configured for this environment (the links then hide).
   */
  shopUrl(slug?: string | null): string | null {
    const base = (userSiteUrl ?? '').replace(/\/+$/, '');
    return base && slug ? `${base}/brands/${encodeURIComponent(slug)}` : null;
  }

  get previewUrl(): string {
    const slug = this.form.get('slug')?.value || '…';
    return `/brands/${slug}`;
  }

  get metaTitlePreview(): string {
    return this.form.get('metaTitle')?.value || 'Page title not set';
  }

  get metaDescriptionPreview(): string {
    return this.form.get('metaDescription')?.value || 'What someone sees under the link on Google.';
  }

  /** How many characters are left of the 155 Google will show. */
  get metaDescriptionLeft(): number {
    return 155 - ((this.form.get('metaDescription')?.value ?? '').length);
  }

  consentLabel(status: BrandProjectConsentStatus): string {
    return {
      [BrandProjectConsentStatus.PhotoAndConsent]: 'Photo + consent',
      [BrandProjectConsentStatus.WaitingOnConsent]: 'Waiting on consent',
      [BrandProjectConsentStatus.NoPhoto]: 'No photo',
    }[status];
  }

  /** Shown in the certificates table — an expiry inside 60 days is called out. */
  expiryNote(row: AbstractControl): { text: string; warn: boolean } {
    if (row.get('noExpiry')?.value) return { text: 'No expiry', warn: false };
    const raw = row.get('expiresOn')?.value;
    if (!raw) return { text: '—', warn: false };

    const when = new Date(raw);
    const days = Math.ceil((when.getTime() - Date.now()) / 86400000);
    if (days < 0) return { text: 'Expired', warn: true };
    if (days <= 60) return { text: `In ${days} days`, warn: true };
    return { text: when.toLocaleDateString(), warn: false };
  }
}
