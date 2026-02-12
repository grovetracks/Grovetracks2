import {
  Component, ElementRef, OnDestroy,
  afterNextRender, effect, input, viewChild
} from '@angular/core';
import { Composition } from '../models/gallery.models';
import { CANVAS_DEFAULTS } from '../rendering/canvas.defaults';

@Component({
  selector: 'app-doodle-canvas',
  template: `
    <div #container class="w-full h-full flex items-center justify-center">
      <canvas #canvas class="block max-w-full max-h-full"></canvas>
    </div>
  `,
  styles: [`:host { display: block; width: 100%; height: 100%; }`]
})
export class DoodleCanvasComponent implements OnDestroy {
  readonly composition = input.required<Composition>();
  readonly strokeColor = input<string>(CANVAS_DEFAULTS.strokeColor);
  readonly backgroundColor = input<string>(CANVAS_DEFAULTS.backgroundColor);
  readonly strokeWidth = input<number>(CANVAS_DEFAULTS.strokeWidth);

  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly containerRef = viewChild.required<ElementRef<HTMLDivElement>>('container');
  private resizeObserver: ResizeObserver | null = null;

  constructor() {
    afterNextRender(() => {
      this.setupResizeObserver();
      this.render();
    });

    effect(() => {
      this.composition();
      this.strokeColor();
      this.backgroundColor();
      this.strokeWidth();
      this.render();
    });
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  private setupResizeObserver(): void {
    const container = this.containerRef()?.nativeElement;
    if (!container) return;

    this.resizeObserver = new ResizeObserver(() => this.render());
    this.resizeObserver.observe(container);
  }

  private render(): void {
    const canvas = this.canvasRef()?.nativeElement;
    const container = this.containerRef()?.nativeElement;
    if (!canvas || !container) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const comp = this.composition();
    const containerWidth = container.clientWidth;
    const containerHeight = container.clientHeight;
    if (containerWidth === 0 || containerHeight === 0) return;

    const compositionRatio = comp.width / comp.height;
    const containerRatio = containerWidth / containerHeight;

    let drawWidth: number;
    let drawHeight: number;

    if (containerRatio > compositionRatio) {
      drawHeight = containerHeight;
      drawWidth = containerHeight * compositionRatio;
    } else {
      drawWidth = containerWidth;
      drawHeight = containerWidth / compositionRatio;
    }

    const dpr = window.devicePixelRatio || 1;
    canvas.width = drawWidth * dpr;
    canvas.height = drawHeight * dpr;
    canvas.style.width = `${drawWidth}px`;
    canvas.style.height = `${drawHeight}px`;
    ctx.scale(dpr, dpr);

    ctx.fillStyle = this.backgroundColor();
    ctx.fillRect(0, 0, drawWidth, drawHeight);

    ctx.strokeStyle = this.strokeColor();
    ctx.lineWidth = this.strokeWidth();
    ctx.lineCap = CANVAS_DEFAULTS.lineCap;
    ctx.lineJoin = CANVAS_DEFAULTS.lineJoin;

    for (const fragment of comp.doodleFragments) {
      for (const stroke of fragment.strokes) {
        const xs = stroke.data[0];
        const ys = stroke.data[1];
        if (!xs || !ys || xs.length === 0) continue;

        ctx.beginPath();
        ctx.moveTo(xs[0] * drawWidth, ys[0] * drawHeight);

        for (let i = 1; i < xs.length; i++) {
          ctx.lineTo(xs[i] * drawWidth, ys[i] * drawHeight);
        }

        ctx.stroke();
      }
    }
  }
}
