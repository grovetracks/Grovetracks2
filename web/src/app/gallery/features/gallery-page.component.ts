import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { GalleryService } from '../data-access/gallery.service';
import { EngagementService } from '../data-access/engagement.service';
import { DoodleWithComposition } from '../models/gallery.models';
import { DoodleCanvasComponent } from '../ui/doodle-canvas.component';
import { EngagementButtonsComponent } from '../ui/engagement-buttons.component';

const DISPLAY_SIZE = 24;
const BUFFER_THRESHOLD = 6;

@Component({
  selector: 'app-gallery-page',
  imports: [DoodleCanvasComponent, EngagementButtonsComponent],
  template: `
    <div class="min-h-screen bg-slate-900 p-6">
      <div class="max-w-7xl mx-auto">
        <h1 class="text-3xl font-bold text-teal-400 mb-6">Quick Draw Gallery</h1>

        <div class="mb-8">
          <label class="block text-sm font-medium text-slate-400 mb-2">
            Select a word
          </label>
          <div class="flex flex-wrap gap-2 max-h-48 overflow-y-auto">
            @for (word of words(); track word) {
              <button
                class="px-4 py-2 rounded-lg text-sm font-medium transition-colors"
                [class]="word === selectedWord()
                  ? 'bg-teal-500 text-slate-900'
                  : 'bg-slate-800 text-slate-300 hover:bg-slate-700'"
                (click)="selectWord(word)">
                {{ word }}
              </button>
            }
          </div>
        </div>

        @if (loading()) {
          <div class="flex justify-center py-12">
            <p class="text-slate-400 text-lg">Loading doodles...</p>
          </div>
        }

        @if (error()) {
          <div class="bg-red-900/30 border border-red-700 rounded-lg p-4 mb-6">
            <p class="text-red-400">{{ error() }}</p>
          </div>
        }

        @if (!loading() && items().length > 0) {
          <p class="text-slate-500 text-sm mb-4">
            {{ items().length }} doodles for "{{ selectedWord() }}"
          </p>
          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            @for (item of items(); track item.doodle.keyId) {
              <div
                class="rounded-lg overflow-hidden cursor-pointer
                       ring-1 ring-slate-700 hover:ring-teal-500 transition-all
                       hover:scale-[1.02]">
                <div
                  class="aspect-square"
                  (click)="navigateToDoodle(item.doodle.keyId)">
                  <app-doodle-canvas [composition]="item.composition" />
                </div>
                <app-engagement-buttons
                  (engaged)="onEngage(item.doodle.keyId, $event)" />
              </div>
            }
          </div>
        }

        @if (!loading() && selectedWord() && items().length === 0 && !error()) {
          <div class="flex justify-center py-12">
            <p class="text-slate-500">No doodles found for "{{ selectedWord() }}"</p>
          </div>
        }
      </div>
    </div>
  `
})
export class GalleryPageComponent implements OnInit {
  private readonly galleryService = inject(GalleryService);
  private readonly engagementService = inject(EngagementService);
  private readonly router = inject(Router);

  protected readonly words = signal<ReadonlyArray<string>>([]);
  protected readonly selectedWord = signal<string | null>(null);
  protected readonly items = signal<DoodleWithComposition[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  private readonly buffer = signal<DoodleWithComposition[]>([]);
  private readonly hasMore = signal(true);
  private readonly refilling = signal(false);

  ngOnInit(): void {
    this.galleryService.getDistinctWords().subscribe({
      next: (words) => {
        this.words.set(words);
        if (words.length > 0) {
          this.selectWord(words[0]);
        }
      },
      error: () => this.error.set('Failed to load word list')
    });
  }

  selectWord(word: string): void {
    this.selectedWord.set(word);
    this.loading.set(true);
    this.error.set(null);
    this.items.set([]);
    this.buffer.set([]);
    this.hasMore.set(true);
    this.refilling.set(false);

    this.galleryService.getGalleryPageExcludingEngaged(word, 48).subscribe({
      next: (page) => {
        const allItems = [...page.items];
        this.items.set(allItems.slice(0, DISPLAY_SIZE));
        this.buffer.set(allItems.slice(DISPLAY_SIZE));
        this.hasMore.set(page.hasMore);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(`Failed to load doodles for "${word}"`);
        this.loading.set(false);
      }
    });
  }

  onEngage(keyId: string, score: number): void {
    this.engagementService.createEngagement({ keyId, score }).subscribe({
      next: () => {
        const currentItems = [...this.items()];
        const currentBuffer = [...this.buffer()];
        const index = currentItems.findIndex(item => item.doodle.keyId === keyId);

        if (index === -1) return;

        if (currentBuffer.length > 0) {
          currentItems[index] = currentBuffer.shift()!;
          this.items.set(currentItems);
          this.buffer.set(currentBuffer);
        } else {
          currentItems.splice(index, 1);
          this.items.set(currentItems);
        }

        if (currentBuffer.length < BUFFER_THRESHOLD && this.hasMore() && !this.refilling()) {
          this.refillBuffer();
        }
      },
      error: () => this.error.set('Failed to save engagement')
    });
  }

  navigateToDoodle(keyId: string): void {
    this.router.navigate(['/gallery', keyId]);
  }

  private refillBuffer(): void {
    const word = this.selectedWord();
    if (!word || this.refilling()) return;

    this.refilling.set(true);

    this.galleryService.getGalleryPageExcludingEngaged(word, DISPLAY_SIZE).subscribe({
      next: (page) => {
        const existingKeyIds = new Set([
          ...this.items().map(i => i.doodle.keyId),
          ...this.buffer().map(i => i.doodle.keyId)
        ]);

        const newItems = page.items.filter(
          item => !existingKeyIds.has(item.doodle.keyId)
        );

        this.buffer.update(current => [...current, ...newItems]);
        this.hasMore.set(page.hasMore);
        this.refilling.set(false);
      },
      error: () => this.refilling.set(false)
    });
  }
}
