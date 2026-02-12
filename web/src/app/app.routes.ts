import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'gallery',
    loadComponent: () =>
      import('./gallery/features/gallery-page.component')
        .then(m => m.GalleryPageComponent)
  },
  {
    path: 'gallery/:keyId',
    loadComponent: () =>
      import('./gallery/features/doodle-detail.component')
        .then(m => m.DoodleDetailComponent)
  },
  {
    path: 'seeds',
    loadComponent: () =>
      import('./seeds/features/seeds-page.component')
        .then(m => m.SeedsPageComponent)
  },
  {
    path: 'seeds/:id',
    loadComponent: () =>
      import('./seeds/features/seed-detail.component')
        .then(m => m.SeedDetailComponent)
  },
  { path: '', redirectTo: 'gallery', pathMatch: 'full' }
];
