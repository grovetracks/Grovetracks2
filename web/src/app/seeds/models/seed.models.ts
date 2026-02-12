import { Composition } from '../../gallery/models/gallery.models';

export type SourceTypeFilter = 'all' | 'curated' | 'generated' | 'ai-generated';

export interface SeedCompositionSummary {
  readonly id: string;
  readonly word: string;
  readonly qualityScore: number;
  readonly strokeCount: number;
  readonly totalPointCount: number;
  readonly curatedAt: string;
  readonly sourceType: string;
  readonly generationMethod: string | null;
}

export interface SeedCompositionWithData {
  readonly summary: SeedCompositionSummary;
  readonly composition: Composition;
}

export interface SeedCompositionPage {
  readonly items: ReadonlyArray<SeedCompositionWithData>;
  readonly totalCount: number;
}
