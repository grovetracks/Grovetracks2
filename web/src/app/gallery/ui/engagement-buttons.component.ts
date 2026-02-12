import { Component, output } from '@angular/core';

@Component({
  selector: 'app-engagement-buttons',
  template: `
    <div class="flex gap-1 justify-center py-1" (click)="$event.stopPropagation()">
      <button
        class="flex-1 px-2 py-1 rounded text-xs font-medium transition-colors
               bg-red-900/60 text-red-300 hover:bg-red-700 hover:text-red-100"
        (click)="engaged.emit(0.0)">
        Negative
      </button>
      <button
        class="flex-1 px-2 py-1 rounded text-xs font-medium transition-colors
               bg-slate-700/60 text-slate-300 hover:bg-slate-600 hover:text-slate-100"
        (click)="engaged.emit(0.25)">
        Neutral
      </button>
      <button
        class="flex-1 px-2 py-1 rounded text-xs font-medium transition-colors
               bg-emerald-900/60 text-emerald-300 hover:bg-emerald-700 hover:text-emerald-100"
        (click)="engaged.emit(1.0)">
        Positive
      </button>
    </div>
  `
})
export class EngagementButtonsComponent {
  readonly engaged = output<number>();
}
