import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { SeedCompositionPage, SeedCompositionWithData, SourceTypeFilter } from '../models/seed.models';

@Injectable({ providedIn: 'root' })
export class SeedService {
  private readonly api = inject(ApiService);

  getDistinctWords(sourceType: SourceTypeFilter = 'all'): Observable<ReadonlyArray<string>> {
    const sourceParam = sourceType === 'all' ? '' : `?sourceType=${sourceType}`;
    return this.api.get<ReadonlyArray<string>>(`seed-compositions/words${sourceParam}`);
  }

  getByWord(word: string, limit: number = 24, sourceType: SourceTypeFilter = 'all'): Observable<SeedCompositionPage> {
    const sourceParam = sourceType === 'all' ? '' : `&sourceType=${sourceType}`;
    return this.api.get<SeedCompositionPage>(
      `seed-compositions/word/${encodeURIComponent(word)}?limit=${limit}${sourceParam}`
    );
  }

  getById(id: string): Observable<SeedCompositionWithData> {
    return this.api.get<SeedCompositionWithData>(
      `seed-compositions/${encodeURIComponent(id)}`
    );
  }

  getTotalCount(sourceType: SourceTypeFilter = 'all'): Observable<number> {
    const sourceParam = sourceType === 'all' ? '' : `?sourceType=${sourceType}`;
    return this.api.get<number>(`seed-compositions/count${sourceParam}`);
  }
}
