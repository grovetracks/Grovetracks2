import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { SeedService } from '../data-access/seed.service';
import { SeedCompositionWithData } from '../models/seed.models';
import { DoodleCanvasComponent } from '../../gallery/ui/doodle-canvas.component';

@Component({
  selector: 'app-seed-detail',
  imports: [DoodleCanvasComponent, DatePipe, DecimalPipe],
  template: `
    <div class="min-h-screen bg-slate-900 p-6">
      <div class="max-w-4xl mx-auto">
        <button
          class="text-amber-400 hover:text-amber-300 mb-6 flex items-center gap-2
                 transition-colors"
          (click)="goBack()">
          <span class="text-xl">&larr;</span>
          <span>Back to Seeds</span>
        </button>

        @if (loading()) {
          <div class="flex justify-center py-12">
            <p class="text-slate-400 text-lg">Loading seed composition...</p>
          </div>
        }

        @if (error()) {
          <div class="bg-red-900/30 border border-red-700 rounded-lg p-4">
            <p class="text-red-400">{{ error() }}</p>
          </div>
        }

        @if (data(); as d) {
          <div class="aspect-[4/3] w-full rounded-lg overflow-hidden
                      ring-1 ring-slate-700 mb-6">
            <app-doodle-canvas [composition]="d.composition" />
          </div>

          <div class="bg-slate-800 rounded-lg p-6">
            <h2 class="text-2xl font-bold text-amber-400 mb-4 capitalize">
              {{ d.summary.word }}
            </h2>
            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span class="text-slate-500">Quality Score</span>
                <p class="text-slate-300">{{ d.summary.qualityScore | number:'1.4-4' }}</p>
              </div>
              <div>
                <span class="text-slate-500">Source Type</span>
                <p class="text-slate-300 capitalize">{{ d.summary.sourceType }}</p>
              </div>
              <div>
                <span class="text-slate-500">Strokes</span>
                <p class="text-slate-300">{{ d.summary.strokeCount }}</p>
              </div>
              <div>
                <span class="text-slate-500">Total Points</span>
                <p class="text-slate-300">{{ d.summary.totalPointCount }}</p>
              </div>
              <div>
                <span class="text-slate-500">Dimensions</span>
                <p class="text-slate-300">{{ d.composition.width }} x {{ d.composition.height }}</p>
              </div>
              @if (d.summary.generationMethod) {
                <div>
                  <span class="text-slate-500">Generation Method</span>
                  <p class="text-slate-300">{{ d.summary.generationMethod }}</p>
                </div>
              }
              <div>
                <span class="text-slate-500">Tags</span>
                <p class="text-slate-300">{{ d.composition.tags.join(', ') }}</p>
              </div>
              <div>
                <span class="text-slate-500">Curated</span>
                <p class="text-slate-300">{{ d.summary.curatedAt | date:'mediumDate' }}</p>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class SeedDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly seedService = inject(SeedService);

  protected readonly data = signal<SeedCompositionWithData | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('No seed composition ID provided');
      this.loading.set(false);
      return;
    }

    this.seedService.getById(id).subscribe({
      next: (result) => {
        this.data.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load seed composition');
        this.loading.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/seeds']);
  }
}
