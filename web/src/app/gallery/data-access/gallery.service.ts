import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { DoodleWithComposition, GalleryPage } from '../models/gallery.models';

@Injectable({ providedIn: 'root' })
export class GalleryService {
  private readonly api = inject(ApiService);

  getDistinctWords(): Observable<ReadonlyArray<string>> {
    return this.api.get<ReadonlyArray<string>>('doodles/words');
  }

  getGalleryPage(word: string, limit: number = 24): Observable<GalleryPage> {
    return this.api.get<GalleryPage>(
      `doodles/word/${encodeURIComponent(word)}?limit=${limit}`
    );
  }

  getGalleryPageExcludingEngaged(word: string, limit: number = 48): Observable<GalleryPage> {
    return this.api.get<GalleryPage>(
      `doodles/word/${encodeURIComponent(word)}?limit=${limit}&excludeEngaged=true`
    );
  }

  getDoodleComposition(keyId: string): Observable<DoodleWithComposition> {
    return this.api.get<DoodleWithComposition>(
      `doodles/${encodeURIComponent(keyId)}/composition`
    );
  }
}
