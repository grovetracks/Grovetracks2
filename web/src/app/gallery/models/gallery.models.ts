export interface DoodleSummary {
  readonly keyId: string;
  readonly word: string;
  readonly countryCode: string;
  readonly timestamp: string;
  readonly recognized: boolean;
}

export interface StrokeData {
  readonly data: ReadonlyArray<ReadonlyArray<number>>;
}

export interface DoodleFragmentData {
  readonly strokes: ReadonlyArray<StrokeData>;
}

export interface Composition {
  readonly width: number;
  readonly height: number;
  readonly doodleFragments: ReadonlyArray<DoodleFragmentData>;
  readonly tags: ReadonlyArray<string>;
}

export interface DoodleWithComposition {
  readonly doodle: DoodleSummary;
  readonly composition: Composition;
}

export interface GalleryPage {
  readonly items: ReadonlyArray<DoodleWithComposition>;
  readonly totalCount: number;
  readonly hasMore: boolean;
}

export interface CreateEngagementRequest {
  readonly keyId: string;
  readonly score: number;
}

export interface EngagementResponse {
  readonly keyId: string;
  readonly score: number;
  readonly engagedAt: string;
}
