import { Injectable } from '@angular/core';
import { HttpClient, HttpEventType, HttpResponse } from '@angular/common/http';
import { Observable, filter, map } from 'rxjs';
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

  private readonly videoUrl = `${environment.apis['default'].url}/api/app/videos/upload`;

  /**
   * Uploads a video and reports progress while it goes up.
   * Emits `{ progress }` as the bytes move and a final `{ url }` when Cloudinary answers.
   */
  uploadVideo(file: File, folder = 'DymoEnergy/videos'): Observable<{ progress: number; url?: string }> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', folder);

    return this.http
      .post<{ url: string }>(this.videoUrl, formData, { reportProgress: true, observe: 'events' })
      .pipe(
        filter(
          e => e.type === HttpEventType.UploadProgress || e.type === HttpEventType.Response
        ),
        map(e => {
          if (e.type === HttpEventType.UploadProgress) {
            const total = e.total ?? file.size;
            return { progress: total ? Math.round((e.loaded / total) * 100) : 0 };
          }
          return { progress: 100, url: (e as HttpResponse<{ url: string }>).body?.url };
        })
      );
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
