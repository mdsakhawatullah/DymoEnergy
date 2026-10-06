import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { timeout } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { UserSiteSettingService } from '../../proxy/user-site-settings/user-site-setting.service';
import { UserSiteSettingDto } from '../../proxy/user-site-settings/models';
import { ImageUploadService } from '../../proxy/image-upload/image-upload.service';

/** Starting copy for the About page — shown on the storefront until the owner changes it. */
const ABOUT_DEFAULTS = {
  eyebrow: 'About Dymo Energy',
  heading: 'Clean, reliable power for every Bangladeshi home and business.',
  story: 'We started with a simple frustration: load-shedding and rising bills, while the sun shines most of the year. Today our engineers design, supply and service solar systems across the country.',
  heroImageUrl: '',
  heroCaption: '',
  valuesEyebrow: 'What we believe',
  valuesHeading: 'How we work',
  values: [
    { title: 'Honest sizing', text: 'We recommend the system you need — not the biggest one we can sell.' },
    { title: 'Genuine equipment', text: 'Every product is traceable by serial number and backed locally.' },
    { title: 'Safe installation', text: 'Proper earthing, breakers and cable sizing on every job.' },
    { title: 'Here for 25 years', text: 'Service and warranty for as long as your panels run.' },
  ],
  teamEyebrow: 'Our team',
  teamHeading: 'The people behind your system',
  certsHeading: 'Certifications & partners',
  showroomTitle: 'Visit our showroom',
  showroomText: 'See panels, inverters and batteries in person.',
};

@Component({
  selector: 'app-user-site-settings',
  templateUrl: './user-site-settings.component.html',
  styleUrl: './user-site-settings.component.css',
  imports: [SharedModule],
})
export class UserSiteSettingsComponent implements OnInit {
  /** Index of the settings section shown on the right. */
  tab = 0;


  form!: FormGroup;
  loading = false;
  /** True when the list call failed, so we do not offer to create a duplicate record. */
  loadFailed = false;
  /** Shown under the spinner so a slow load says what it is waiting for. */
  loadStep = '';
  saving  = false;

  /** null = no record yet; number = existing record id */
  existingId: number | null = null;

  get hasRecord(): boolean { return this.existingId !== null; }

  get imagesArray(): FormArray {
    return this.form.get('images') as FormArray;
  }

  constructor(
    private fb:      FormBuilder,
    private svc:     UserSiteSettingService,
    private message: NzMessageService,
    private imageUpload: ImageUploadService,
  ) {
    this.buildForm();
    this.patchAbout(null);
  }

  ngOnInit(): void {
    this.loadSetting();
  }

  buildForm(): void {
    this.form = this.fb.group({
      // ── General ──────────────────────────────────────────────────────────
      backgroundImage:        [null],
      description:            [null],
      // ── Brand colours ────────────────────────────────────────────────────
      buttonColor:            [null],
      primaryColor:           [null],
      bodyColor:              [null],
      // ── Backgrounds ──────────────────────────────────────────────────────
      backgroundColor:        [null],
      cardBgColor:            [null],
      // ── Navbar ───────────────────────────────────────────────────────────
      navbarBgColor:          [null],
      navbarTextColor:        [null],
      // ── Sidebar ──────────────────────────────────────────────────────────
      sidebarBgColor:         [null],
      sidebarTextColor:       [null],
      sidebarActiveBgColor:   [null],
      // ── Buttons ──────────────────────────────────────────────────────────
      buttonPrimaryBgColor:   [null],
      buttonPrimaryTextColor: [null],
      // ── Typography ───────────────────────────────────────────────────────
      fontFamily:             [null],
      fontSizeBase:           [null],
      // ── About Section ────────────────────────────────────────────────────
      aboutTitle:             [null],
      aboutDescription:       [null],
      // ── Categories Section ───────────────────────────────────────────────
      categoriesTitle:        [null],
      categoriesDescription:  [null],
      // ── Social Media ─────────────────────────────────────────────────────
      socialLinkedinUrl:      [null],
      socialInstagramUrl:     [null],
      socialFacebookUrl:      [null],
      socialTwitterUrl:       [null],
      socialYoutubeUrl:       [null],
      // ── About page (stored as one JSON string) ─────────────────────────────
      aboutPage: this.fb.group({
        eyebrow: [''], heading: [''], story: [''], heroImageUrl: [''], heroCaption: [''],
        stats: this.fb.array([]),
        valuesEyebrow: [''], valuesHeading: [''], values: this.fb.array([]),
        teamEyebrow: [''], teamHeading: [''], team: this.fb.array([]),
        certsHeading: [''], certs: this.fb.array([]),
        showroomTitle: [''], showroomText: [''],
      }),
      // ── Status ───────────────────────────────────────────────────────────
      isActive: [true],
      // ── Images ───────────────────────────────────────────────────────────
      images: this.fb.array([]),
    });
  }

  // ── Load ──────────────────────────────────────────────────────────────

  loadSetting(): void {
    this.loading = true;
    this.loadStep = 'Asking the server for the active settings…';
    // The active record comes from a lightweight endpoint that returns the full setting in one call.
    this.svc.getActive().pipe(timeout(15000)).subscribe({
      next: active => {
        if (active?.id) {
          this.existingId = active.id;
          this.patchForm(active);
          this.loading = false;
        } else {
          this.loadFromList();
        }
      },
      error: () => this.loadFromList(),
    });
  }

  /** Fallback when there is no active record: take the first saved one. */
  private loadFromList(): void {
    this.loadStep = 'No active record — looking through saved settings…';
    this.svc.getList({ maxResultCount: 1, skipCount: 0 }).pipe(timeout(15000)).subscribe({
      next: result => {
        if (result.totalCount > 0) {
          this.existingId = result.items[0].id;
          this.loadStep = 'Loading the saved record…';
          this.svc.get(result.items[0].id).subscribe({
            next: full => { this.patchForm(full); this.loading = false; },
            error: () => this.failLoad(undefined),
          });
        } else {
          this.loading = false;
        }
      },
      error: err => this.failLoad(err),
    });
  }

  private failLoad(err: any): void {
    this.loading = false;
    this.loadFailed = true;
    this.message.error(`Could not load the settings (${err?.status ?? 'no response'}). Check you are signed in and have the User Site Settings permission.`, { nzDuration: 8000 });
  }

  patchForm(dto: UserSiteSettingDto): void {
    this.form.patchValue({
      backgroundImage:        dto.backgroundImage,
      description:            dto.description,
      buttonColor:            dto.buttonColor,
      primaryColor:           dto.primaryColor,
      bodyColor:              dto.bodyColor,
      backgroundColor:        dto.backgroundColor,
      cardBgColor:            dto.cardBgColor,
      navbarBgColor:          dto.navbarBgColor,
      navbarTextColor:        dto.navbarTextColor,
      sidebarBgColor:         dto.sidebarBgColor,
      sidebarTextColor:       dto.sidebarTextColor,
      sidebarActiveBgColor:   dto.sidebarActiveBgColor,
      buttonPrimaryBgColor:   dto.buttonPrimaryBgColor,
      buttonPrimaryTextColor: dto.buttonPrimaryTextColor,
      fontFamily:             dto.fontFamily,
      fontSizeBase:           dto.fontSizeBase,
      aboutTitle:             dto.aboutTitle,
      aboutDescription:       dto.aboutDescription,
      categoriesTitle:        dto.categoriesTitle,
      categoriesDescription:  dto.categoriesDescription,
      socialLinkedinUrl:      dto.socialLinkedinUrl,
      socialInstagramUrl:     dto.socialInstagramUrl,
      socialFacebookUrl:      dto.socialFacebookUrl,
      socialTwitterUrl:       dto.socialTwitterUrl,
      socialYoutubeUrl:       dto.socialYoutubeUrl,
      isActive:               dto.isActive,
    });

    this.patchAbout(dto.aboutPageContent);

    // Rebuild images FormArray
    this.imagesArray.clear();
    (dto.images ?? []).forEach(img => this.imagesArray.push(this.buildImageGroup(img)));
  }

  // ── Images FormArray ──────────────────────────────────────────────────

  buildImageGroup(img?: Partial<UserSiteSettingDto['images'][0]>): FormGroup {
    return this.fb.group({
      id:           [img?.id ?? null],
      imageUrl:     [img?.imageUrl ?? null],
      title:        [img?.title ?? null],
      altText:      [img?.altText ?? null],
      displayOrder: [img?.displayOrder ?? this.imagesArray.length],
      isActive:     [img?.isActive ?? true],
    });
  }

  addImage(): void {
    this.imagesArray.push(this.buildImageGroup());
  }

  removeImage(index: number): void {
    this.imagesArray.removeAt(index);
  }

  // ── Save ──────────────────────────────────────────────────────────────

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;
    const { aboutPage, ...rest } = this.form.value;
    const payload = { ...rest, aboutPageContent: this.aboutJson(aboutPage) };

    const request$ = this.existingId
      ? this.svc.update(this.existingId, payload)
      : this.svc.create(payload);

    request$.subscribe({
      next: result => {
        this.saving     = false;
        this.existingId = result.id;
        this.message.success(this.existingId ? 'Settings updated successfully.' : 'Settings created successfully.');
      },
      error: () => {
        this.saving = false;
        this.message.error('Something went wrong. Please try again.');
      },
    });
  }

  // ── About page ────────────────────────────────────────────────────────

  get aboutGroup(): FormGroup { return this.form.get('aboutPage') as FormGroup; }
  aboutArray(name: 'stats' | 'values' | 'team' | 'certs'): FormArray { return this.aboutGroup.get(name) as FormArray; }

  private row(fields: Record<string, string>): FormGroup {
    return this.fb.group(Object.fromEntries(Object.entries(fields).map(([k, v]) => [k, [v]])));
  }

  addAbout(name: 'stats' | 'values' | 'team' | 'certs'): void {
    const blank = {
      stats:  { value: '', label: '' },
      values: { title: '', text: '' },
      team:   { name: '', role: '', photoUrl: '' },
      certs:  { name: '', logoUrl: '' },
    }[name];
    this.aboutArray(name).push(this.row(blank));
  }

  removeAbout(name: 'stats' | 'values' | 'team' | 'certs', i: number): void {
    this.aboutArray(name).removeAt(i);
  }

  /** Fills the About tab from the saved JSON, or from the starting copy when nothing is saved yet. */
  private patchAbout(json?: string | null): void {
    let saved: any = {};
    try { saved = json ? JSON.parse(json) : {}; } catch { saved = {}; }
    const d = ABOUT_DEFAULTS;
    const pick = (k: keyof typeof d) => (saved[k] ?? d[k]) as any;

    this.aboutGroup.patchValue({
      eyebrow: pick('eyebrow'), heading: pick('heading'), story: pick('story'),
      heroImageUrl: saved.heroImageUrl ?? '', heroCaption: saved.heroCaption ?? '',
      valuesEyebrow: pick('valuesEyebrow'), valuesHeading: pick('valuesHeading'),
      teamEyebrow: pick('teamEyebrow'), teamHeading: pick('teamHeading'),
      certsHeading: pick('certsHeading'),
      showroomTitle: pick('showroomTitle'), showroomText: pick('showroomText'),
    });

    const fill = (name: 'stats' | 'values' | 'team' | 'certs', rows: any[], make: (r: any) => Record<string, string>) => {
      const arr = this.aboutArray(name);
      arr.clear();
      rows.forEach(r => arr.push(this.row(make(r))));
    };
    const stats = saved.stats?.length ? saved.stats : Array.from({ length: 4 }, () => ({}));
    fill('stats',  stats, r => ({ value: r.value ?? '', label: r.label ?? '' }));
    fill('values', saved.values ?? d.values, r => ({ title: r.title ?? '', text: r.text ?? '' }));
    fill('team',   saved.team ?? [], r => ({ name: r.name ?? '', role: r.role ?? '', photoUrl: r.photoUrl ?? '' }));
    fill('certs',  saved.certs ?? [], r => ({ name: r.name ?? '', logoUrl: r.logoUrl ?? '' }));
  }

  /** Blank rows are dropped so the storefront only shows what was filled in. */
  private aboutJson(v: any): string {
    const keep = (rows: any[], key: string) => (rows ?? []).filter(r => (r[key] ?? '').toString().trim());
    return JSON.stringify({
      ...v,
      stats:  keep(v.stats, 'value'),
      values: keep(v.values, 'title'),
      team:   keep(v.team, 'name'),
      certs:  keep(v.certs, 'name'),
    });
  }

  /** Opens a file picker, uploads the image and puts its URL into the given control. */
  uploadInto(control: AbstractControl | null): void {
    if (!control) return;
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/jpeg,image/png,image/webp';
    input.onchange = () => {
      const file = input.files?.[0];
      if (!file) return;
      if (file.size > 2 * 1024 * 1024) { this.message.error('Pick an image under 2 MB.'); return; }
      this.imageUpload.uploadImage(file, 'about').subscribe({
        next: url => { control.setValue(url); this.message.success('Image uploaded.'); },
        error: () => this.message.error('Could not upload that image.'),
      });
    };
    input.click();
  }

  // ── Colour helpers ────────────────────────────────────────────────────
  /** Sync colour-picker → text input */
  onColorPickerChange(controlName: string, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.form.get(controlName)?.setValue(value, { emitEvent: false });
  }

  /** Hex value for the native colour picker (falls back to #000000) */
  colorValue(controlName: string): string {
    const v = this.form.get(controlName)?.value;
    return v && /^#[0-9A-Fa-f]{6}$/.test(v) ? v : '#000000';
  }
}
