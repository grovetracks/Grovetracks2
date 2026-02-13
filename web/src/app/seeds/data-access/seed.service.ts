import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { SeedCompositionPage, SeedCompositionWithData, SourceTypeFilter } from '../models/seed.models';

@Injectable({ providedIn: 'root' })
export class SeedService {
  private readonly api = inject(ApiService);

  getDistinctWords(sourceType: SourceTypeFilter = 'all', generationMethod: string | null = null): Observable<ReadonlyArray<string>> {
    const params = this.buildParams({ sourceType, generationMethod });
    return this.api.get<ReadonlyArray<string>>(`seed-compositions/words${params}`);
  }

  getByWord(word: string, limit: number = 24, sourceType: SourceTypeFilter = 'all', generationMethod: string | null = null): Observable<SeedCompositionPage> {
    const params = this.buildParams({ sourceType, generationMethod, limit });
    return this.api.get<SeedCompositionPage>(
      `seed-compositions/word/${encodeURIComponent(word)}${params}`
    );
  }

  getById(id: string): Observable<SeedCompositionWithData> {
    return this.api.get<SeedCompositionWithData>(
      `seed-compositions/${encodeURIComponent(id)}`
    );
  }

  getTotalCount(sourceType: SourceTypeFilter = 'all', generationMethod: string | null = null): Observable<number> {
    const params = this.buildParams({ sourceType, generationMethod });
    return this.api.get<number>(`seed-compositions/count${params}`);
  }

  getDistinctGenerationMethods(sourceType: SourceTypeFilter = 'all'): Observable<ReadonlyArray<string>> {
    const params = sourceType === 'all' ? '' : `?sourceType=${sourceType}`;
    return this.api.get<ReadonlyArray<string>>(`seed-compositions/generation-methods${params}`);
  }

  private buildParams(options: { sourceType?: SourceTypeFilter; generationMethod?: string | null; limit?: number }): string {
    const parts: string[] = [];
    if (options.limit) parts.push(`limit=${options.limit}`);
    if (options.sourceType && options.sourceType !== 'all') parts.push(`sourceType=${options.sourceType}`);
    if (options.generationMethod) parts.push(`generationMethod=${encodeURIComponent(options.generationMethod)}`);
    return parts.length > 0 ? `?${parts.join('&')}` : '';
  }
}
