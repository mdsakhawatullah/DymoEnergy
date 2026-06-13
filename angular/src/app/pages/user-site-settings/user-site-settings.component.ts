import { Component, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { UserSiteSettingService } from '../../proxy/user-site-settings/user-site-setting.service';
import { UserSiteSettingDto } from '../../proxy/user-site-settings/models';

@Component({
  selector: 'app-user-site-settings',
  templateUrl: './user-site-settings.component.html',
  styleUrl: './user-site-settings.component.css',
  imports: [SharedModule],
})
export class UserSiteSettingsComponent implements OnInit {

  form!: FormGroup;
  loading = false;
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
  ) {
    this.buildForm();
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
      // ── Status ───────────────────────────────────────────────────────────
      isActive: [true],
      // ── Images ───────────────────────────────────────────────────────────
      images: this.fb.array([]),
    });
  }

  // ── Load ──────────────────────────────────────────────────────────────

  loadSetting(): void {
    this.loading = true;
    this.svc.getList({ maxResultCount: 1, skipCount: 0 }).subscribe({
      next: result => {
        this.loading = false;
        if (result.totalCount > 0) {
          this.existingId = result.items[0].id;
          this.svc.get(result.items[0].id).subscribe({
            next: full => this.patchForm(full),
          });
        }
      },
      error: () => { this.loading = false; },
    });
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
      isActive:               dto.isActive,
    });

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
    const payload = this.form.value;

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
