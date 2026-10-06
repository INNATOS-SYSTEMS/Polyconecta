import { Routes, UrlMatchResult, UrlSegment } from '@angular/router';

/** Los controles libres (QC-2026-0001) tienen su propio formulario; el resto de folios son OF. */
function controlLibre(segments: UrlSegment[]): UrlMatchResult | null {
  if (segments.length !== 2 || segments[0].path !== 'calidad' || !/^QC-\d{4}-\d{4}$/.test(segments[1].path)) return null;
  return { consumed: segments, posParams: { folio: segments[1] } };
}

export const CALIDAD_ROUTES: Routes = [
  { path: 'calidad', loadComponent: () => import('./calidad-list/calidad-list').then(m => m.CalidadList) },
  { path: 'calidad/nuevo', loadComponent: () => import('./calidad-nuevo/calidad-nuevo').then(m => m.CalidadNuevo) },
  { matcher: controlLibre, loadComponent: () => import('./calidad-libre-form/calidad-libre-form').then(m => m.CalidadLibreForm) },
  { path: 'calidad/:folioOf', loadComponent: () => import('./calidad-form/calidad-form').then(m => m.CalidadForm) },
];
