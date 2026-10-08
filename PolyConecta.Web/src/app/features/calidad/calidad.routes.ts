import { Routes, UrlMatchResult, UrlSegment } from '@angular/router';

/** Los controles libres (QC-2026-0001) tienen su propio formulario; el resto de folios son OF. */
function controlLibre(segments: UrlSegment[]): UrlMatchResult | null {
  if (segments.length !== 1 || !/^QC-\d{4}-\d{4}$/.test(segments[0].path)) return null;
  return { consumed: segments, posParams: { folio: segments[0] } };
}

/** Módulo Calidad, bajo `/calidad` (D-155). */
export const CALIDAD_ROUTES: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./calidad-list/calidad-list').then(m => m.CalidadList) },
  { path: 'nuevo', loadComponent: () => import('./calidad-nuevo/calidad-nuevo').then(m => m.CalidadNuevo) },
  { matcher: controlLibre, loadComponent: () => import('./calidad-libre-form/calidad-libre-form').then(m => m.CalidadLibreForm) },
  { path: ':folioOf', loadComponent: () => import('./calidad-form/calidad-form').then(m => m.CalidadForm) },
];
