import { UrlMatchResult, UrlSegment } from '@angular/router';

/**
 * Equivalente de las rutas comodín de Blazor (`/entregas/{*Folio}`): toma todo lo que sigue al
 * prefijo como un solo folio, para resolver folios con "/" como `PIM/OUT/48214`.
 */
export function folioMatcher(prefijo: string) {
  return (segments: UrlSegment[]): UrlMatchResult | null => {
    if (segments.length < 2 || segments[0].path !== prefijo) return null;
    const folio = segments.slice(1).map(s => s.path).join('/');
    return { consumed: segments, posParams: { folio: new UrlSegment(folio, {}) } };
  };
}
