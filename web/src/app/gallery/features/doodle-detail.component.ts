import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { GalleryService } from '../data-access/gallery.service';
import { DoodleWithComposition } from '../models/gallery.models';
import { DoodleCanvasComponent } from '../ui/doodle-canvas.component';

@Component({
  selector: 'app-doodle-detail',
  imports: [DoodleCanvasComponent, DatePipe],
  template: `
    <div class="min-h-screen bg-slate-900 p-6">
      <div class="max-w-4xl mx-auto">
        <button
          class="text-teal-400 hover:text-teal-300 mb-6 flex items-center gap-2
                 transition-colors"
          (click)="goBack()">
          <span class="text-xl">&larr;</span>
          <span>Back to Gallery</span>
        </button>

        @if (loading()) {
          <div class="flex justify-center py-12">
            <p class="text-slate-400 text-lg">Loading doodle...</p>
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
            <h2 class="text-2xl font-bold text-teal-400 mb-4 capitalize">
              {{ d.doodle.word }}
            </h2>
            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span class="text-slate-500">Country</span>
                <p class="text-slate-300">{{ d.doodle.countryCode }}</p>
              </div>
              <div>
                <span class="text-slate-500">Recognized</span>
                <p class="text-slate-300">{{ d.doodle.recognized ? 'Yes' : 'No' }}</p>
              </div>
              <div>
                <span class="text-slate-500">Dimensions</span>
                <p class="text-slate-300">{{ d.composition.width }} x {{ d.composition.height }}</p>
              </div>
              <div>
                <span class="text-slate-500">Strokes</span>
                <p class="text-slate-300">{{ d.composition.doodleFragments[0]?.strokes?.length ?? 0 }}</p>
              </div>
              <div>
                <span class="text-slate-500">Tags</span>
                <p class="text-slate-300">{{ d.composition.tags.join(', ') }}</p>
              </div>
              <div>
                <span class="text-slate-500">Date</span>
                <p class="text-slate-300">{{ d.doodle.timestamp | date:'mediumDate' }}</p>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class DoodleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly galleryService = inject(GalleryService);

  protected readonly data = signal<DoodleWithComposition | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const keyId = this.route.snapshot.paramMap.get('keyId');
    if (!keyId) {
      this.error.set('No doodle ID provided');
      this.loading.set(false);
      return;
    }

    this.galleryService.getDoodleComposition(keyId).subscribe({
      next: (result) => {
        this.data.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load doodle');
        this.loading.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/gallery']);
  }
}
