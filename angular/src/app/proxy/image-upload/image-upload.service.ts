import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ImageUploadService {
  private readonly apiUrl = `${environment.apis['default'].url}/api/app/images/upload`;

  constructor(private http: HttpClient) {}

  uploadImage(file: File, folder = 'DymoEnergy'): Observable<string> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folder', folder);

    return this.http.post<{ url: string }>(this.apiUrl, formData).pipe(
      map(res => res.url)
    );
  }
}
