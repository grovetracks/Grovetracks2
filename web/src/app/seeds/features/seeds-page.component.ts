import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { SeedService } from '../data-access/seed.service';
import { SeedCompositionWithData, SourceTypeFilter } from '../models/seed.models';
import { DoodleCanvasComponent } from '../../gallery/ui/doodle-canvas.component';

@Component({
  selector: 'app-seeds-page',
  imports: [DoodleCanvasComponent, DecimalPipe],
  template: `
    <div class="min-h-screen bg-slate-900 p-6">
      <div class="max-w-7xl mx-auto">
        <div class="flex items-center justify-between mb-6">
          <h1 class="text-3xl font-bold text-amber-400">Seed Compositions</h1>
          @if (totalCount() > 0) {
            <span class="text-slate-500 text-sm">{{ totalCount() }} seeds</span>
          }
        </div>

        <div class="flex gap-1 mb-6">
          @for (tab of tabs; track tab.value) {
            <button
              class="px-4 py-2 rounded-lg text-sm font-medium transition-colors"
              [class]="tab.value === activeTab()
                ? 'bg-amber-500 text-slate-900'
                : 'bg-slate-800 text-slate-400 hover:bg-slate-700 hover:text-slate-300'"
              (click)="selectTab(tab.value)">
              {{ tab.label }}
            </button>
          }
        </div>

        @if (words().length > 0) {
          <div class="mb-8">
            <label class="block text-sm font-medium text-slate-400 mb-2">
              Categories
            </label>
            <div class="flex flex-wrap gap-2 max-h-48 overflow-y-auto">
              @for (word of words(); track word) {
                <button
                  class="px-4 py-2 rounded-lg text-sm font-medium transition-colors"
                  [class]="word === selectedWord()
                    ? 'bg-amber-500 text-slate-900'
                    : 'bg-slate-800 text-slate-300 hover:bg-slate-700'"
                  (click)="selectWord(word)">
                  {{ word }}
                </button>
              }
            </div>
          </div>
        }

        @if (loading()) {
          <div class="flex justify-center py-12">
            <p class="text-slate-400 text-lg">Loading seed compositions...</p>
          </div>
        }

        @if (error()) {
          <div class="bg-red-900/30 border border-red-700 rounded-lg p-4 mb-6">
            <p class="text-red-400">{{ error() }}</p>
          </div>
        }

        @if (!loading() && items().length > 0) {
          <p class="text-slate-500 text-sm mb-4">
            {{ items().length }} of {{ categoryCount() }}
            seeds for "{{ selectedWord() }}"
          </p>
          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            @for (item of items(); track item.summary.id) {
              <div
                class="rounded-lg overflow-hidden cursor-pointer
                       ring-1 ring-slate-700 hover:ring-amber-500 transition-all
                       hover:scale-[1.02]"
                (click)="navigateToSeed(item.summary.id)">
                <div class="aspect-square">
                  <app-doodle-canvas [composition]="item.composition" />
                </div>
                <div class="px-2 py-1 bg-slate-800/80">
                  <div class="flex justify-between items-center text-xs">
                    <span class="text-amber-400">{{ item.summary.qualityScore | number:'1.2-2' }}</span>
                    @if ((item.summary.sourceType === 'generated' || item.summary.sourceType === 'ai-generated') && item.summary.generationMethod) {
                      <span class="text-slate-500 truncate max-w-[80px]" [title]="item.summary.generationMethod">
                        {{ item.summary.generationMethod }}
                      </span>
                    } @else {
                      <span class="text-slate-500">{{ item.summary.strokeCount }}s / {{ item.summary.totalPointCount }}p</span>
                    }
                  </div>
                </div>
              </div>
            }
          </div>
        }

        @if (!loading() && selectedWord() && items().length === 0 && !error()) {
          <div class="flex justify-center py-12">
            <p class="text-slate-500">
              No seed compositions found for "{{ selectedWord() }}"
            </p>
          </div>
        }

        @if (!loading() && words().length === 0 && !error()) {
          <div class="flex justify-center py-12">
            <div class="text-center">
              <p class="text-slate-400 text-lg mb-2">No seed compositions yet</p>
              <p class="text-slate-600 text-sm">Run the ETL curation command to populate:</p>
              <code class="text-amber-400 text-sm mt-2 block">dotnet run -- curate-simple-doodles</code>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class SeedsPageComponent implements OnInit {
  private readonly seedService = inject(SeedService);
  private readonly router = inject(Router);

  protected readonly words = signal<ReadonlyArray<string>>([]);
  protected readonly selectedWord = signal<string | null>(null);
  protected readonly items = signal<SeedCompositionWithData[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly categoryCount = signal(0);
  protected readonly activeTab = signal<SourceTypeFilter>('all');

  protected readonly tabs: ReadonlyArray<{ readonly label: string; readonly value: SourceTypeFilter }> = [
    { label: 'All', value: 'all' },
    { label: 'Curated', value: 'curated' },
    { label: 'Generated', value: 'generated' },
    { label: 'AI Generated', value: 'ai-generated' }
  ];

  ngOnInit(): void {
    this.loadWordsAndCount(this.activeTab());
  }

  selectTab(tab: SourceTypeFilter): void {
    this.activeTab.set(tab);
    this.selectedWord.set(null);
    this.items.set([]);
    this.categoryCount.set(0);
    this.loadWordsAndCount(tab);
  }

  selectWord(word: string): void {
    this.selectedWord.set(word);
    this.fetchSeeds(word, this.activeTab());
  }

  navigateToSeed(id: string): void {
    this.router.navigate(['/seeds', id]);
  }

  private loadWordsAndCount(sourceType: SourceTypeFilter): void {
    this.error.set(null);

    this.seedService.getDistinctWords(sourceType).subscribe({
      next: (words) => {
        this.words.set(words);
        if (words.length > 0) {
          this.selectWord(words[0]);
        }
      },
      error: () => this.error.set('Failed to load categories')
    });

    this.seedService.getTotalCount(sourceType).subscribe({
      next: (count) => this.totalCount.set(count)
    });
  }

  private fetchSeeds(word: string, sourceType: SourceTypeFilter): void {
    this.loading.set(true);
    this.error.set(null);
    this.items.set([]);

    this.seedService.getByWord(word, 48, sourceType).subscribe({
      next: (page) => {
        this.items.set([...page.items]);
        this.categoryCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(`Failed to load seeds for "${word}"`);
        this.loading.set(false);
      }
    });
  }
}
