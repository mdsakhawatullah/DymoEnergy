import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { AdminSiteSettingService } from '../../proxy/admin-site-settings/admin-site-setting.service';
import { AdminSiteSettingDto } from '../../proxy/admin-site-settings/models';

@Component({
  selector: 'app-admin-site-settings',
  templateUrl: './admin-site-settings.component.html',
  styleUrl: './admin-site-settings.component.css',
  imports: [SharedModule],
})
export class AdminSiteSettingsComponent implements OnInit {

  form!: FormGroup;
  loading = false;
  saving  = false;

  /** null = no record yet; number = existing record id */
  existingId: number | null = null;

  get hasRecord(): boolean { return this.existingId !== null; }

  constructor(
    private fb:      FormBuilder,
    private svc:     AdminSiteSettingService,
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
      siteName:             [null],
      tagline:              [null],
      logoUrl:              [null],
      // ── Brand colours ────────────────────────────────────────────────────
      primaryColor:         [null],
      secondaryColor:       [null],
      accentColor:          [null],
      buttonColor:          [null],
      // ── Text colours ─────────────────────────────────────────────────────
      textColor:            [null],
      textMutedColor:       [null],
      linkColor:            [null],
      // ── Backgrounds ──────────────────────────────────────────────────────
      backgroundColor:      [null],
      cardBgColor:          [null],
      // ── Navbar ───────────────────────────────────────────────────────────
      navbarBgColor:        [null],
      navbarTextColor:      [null],
      // ── Sidebar ──────────────────────────────────────────────────────────
      sidebarBgColor:       [null],
      sidebarTextColor:     [null],
      sidebarActiveBgColor: [null],
      // ── Buttons ──────────────────────────────────────────────────────────
      buttonPrimaryBgColor:   [null],
      buttonPrimaryTextColor: [null],
      // ── Typography ───────────────────────────────────────────────────────
      fontFamily:   [null],
      fontSizeBase: [null],
      // ── Status ───────────────────────────────────────────────────────────
      isActive: [true],
    });
  }

  loadSetting(): void {
    this.loading = true;
    this.svc.getList({ maxResultCount: 1, skipCount: 0 }).subscribe({
      next: result => {
        this.loading = false;
        if (result.totalCount > 0) {
          this.existingId = result.items[0].id;
          this.patchForm(result.items[0]);
        }
      },
      error: () => { this.loading = false; },
    });
  }

  patchForm(dto: AdminSiteSettingDto): void {
    this.form.patchValue({
      siteName:               dto.siteName,
      tagline:                dto.tagline,
      logoUrl:                dto.logoUrl,
      primaryColor:           dto.primaryColor,
      secondaryColor:         dto.secondaryColor,
      accentColor:            dto.accentColor,
      buttonColor:            dto.buttonColor,
      textColor:              dto.textColor,
      textMutedColor:         dto.textMutedColor,
      linkColor:              dto.linkColor,
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
      isActive:               dto.isActive,
    });
  }

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
