import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ImageUploadService {
  private readonly apiUrl = `${environment.apis['default'].url}/api/app/images/upload`;

  constructor(private http: HttpClient) {}

  private readonly pdfUrl = `${environment.apis['default'].url}/api/app/product-files/upload`;

  /** Uploads a product PDF (datasheet / manual) and returns its public URL. */
  uploadProductPdf(file: File): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(this.pdfUrl, formData).pipe(map(res => res.url));
  }

  uploadImage(file: File, folder = 'DymoEnergy'): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', folder);

    return this.http.post<{ url: string }>(this.apiUrl, formData).pipe(
      map(res => res.url)
    );
  }
}
