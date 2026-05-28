import { provideZoneChangeDetection, importProvidersFrom } from "@angular/core";
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import { registerLocaleData } from '@angular/common';
import en from '@angular/common/locales/en';
import { FormsModule } from '@angular/forms';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient } from '@angular/common/http';
import {
  PlusOutline,
  SearchOutline,
  PictureOutline,
  CheckCircleOutline,
  EditOutline,
  FilterOutline,
  ReloadOutline,
  CloseOutline,
} from '@ant-design/icons-angular/icons';

registerLocaleData(en);

bootstrapApplication(AppComponent, {
  ...appConfig,
  providers: [
    provideZoneChangeDetection(),
    ...appConfig.providers,
    provideNzI18n(en_US),
    provideNzIcons([
      PlusOutline,
      SearchOutline,
      PictureOutline,
      CheckCircleOutline,
      EditOutline,
      FilterOutline,
      ReloadOutline,
      CloseOutline,
    ]),
    importProvidersFrom(FormsModule),
    provideAnimationsAsync(),
    provideHttpClient(),
  ],
}).catch(err => console.error(err));
