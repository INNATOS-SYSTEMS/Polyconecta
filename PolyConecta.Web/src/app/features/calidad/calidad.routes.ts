import { Routes, UrlMatchResult, UrlSegment } from '@angular/router';
import { CalidadLibreForm } from './calidad-libre-form/calidad-libre-form';
import { CalidadNuevo } from './calidad-nuevo/calidad-nuevo';
import { CalidadForm } from './calidad-form/calidad-form';
import { CalidadList } from './calidad-list/calidad-list';

/** Los controles libres (QC-2026-0001) tienen su propio formulario; el resto de folios son OF. */
function controlLibre(segments: UrlSegment[]): UrlMatchResult | null {
  if (segments.length !== 2 || segments[0].path !== 'calidad' || !/^QC-\d{4}-\d{4}$/.test(segments[1].path)) return null;
  return { consumed: segments, posParams: { folio: segments[1] } };
}

export const CALIDAD_ROUTES: Routes = [
  { path: 'calidad', component: CalidadList },
  { path: 'calidad/nuevo', component: CalidadNuevo },
  { matcher: controlLibre, component: CalidadLibreForm },
  { path: 'calidad/:folioOf', component: CalidadForm },
];
