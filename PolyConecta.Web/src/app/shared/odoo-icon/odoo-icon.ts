import { Component, computed, input } from '@angular/core';

import LucideArchive from 'lucide/dist/esm/icons/archive.mjs';
import LucideArrowLeft from 'lucide/dist/esm/icons/arrow-left.mjs';
import LucideBell from 'lucide/dist/esm/icons/bell.mjs';
import LucideBoxes from 'lucide/dist/esm/icons/boxes.mjs';
import LucideCalendar from 'lucide/dist/esm/icons/calendar.mjs';
import LucideCheck from 'lucide/dist/esm/icons/check.mjs';
import LucideChevronDown from 'lucide/dist/esm/icons/chevron-down.mjs';
import LucideChevronLeft from 'lucide/dist/esm/icons/chevron-left.mjs';
import LucideChevronRight from 'lucide/dist/esm/icons/chevron-right.mjs';
import LucideCircleAlert from 'lucide/dist/esm/icons/circle-alert.mjs';
import LucideCircleCheck from 'lucide/dist/esm/icons/circle-check.mjs';
import LucideCircleCheckBig from 'lucide/dist/esm/icons/circle-check-big.mjs';
import LucideCircleUser from 'lucide/dist/esm/icons/circle-user.mjs';
import LucideCircleX from 'lucide/dist/esm/icons/circle-x.mjs';
import LucideClock from 'lucide/dist/esm/icons/clock.mjs';
import LucideCloudCheck from 'lucide/dist/esm/icons/cloud-check.mjs';
import LucideColumns3 from 'lucide/dist/esm/icons/columns-3.mjs';
import LucideCornerDownRight from 'lucide/dist/esm/icons/corner-down-right.mjs';
import LucideDownload from 'lucide/dist/esm/icons/download.mjs';
import LucideEllipsis from 'lucide/dist/esm/icons/ellipsis.mjs';
import LucideFileSpreadsheet from 'lucide/dist/esm/icons/file-spreadsheet.mjs';
import LucideFileText from 'lucide/dist/esm/icons/file-text.mjs';
import LucideFunnel from 'lucide/dist/esm/icons/funnel.mjs';
import LucideGripVertical from 'lucide/dist/esm/icons/grip-vertical.mjs';
import LucideGroup from 'lucide/dist/esm/icons/group.mjs';
import LucideLayers from 'lucide/dist/esm/icons/layers.mjs';
import LucideLayoutGrid from 'lucide/dist/esm/icons/layout-grid.mjs';
import LucideLink from 'lucide/dist/esm/icons/link.mjs';
import LucideList from 'lucide/dist/esm/icons/list.mjs';
import LucideLoaderCircle from 'lucide/dist/esm/icons/loader-circle.mjs';
import LucideLogOut from 'lucide/dist/esm/icons/log-out.mjs';
import LucideNetwork from 'lucide/dist/esm/icons/network.mjs';
import LucideOctagonX from 'lucide/dist/esm/icons/octagon-x.mjs';
import LucidePackage from 'lucide/dist/esm/icons/package.mjs';
import LucidePackageOpen from 'lucide/dist/esm/icons/package-open.mjs';
import LucidePenLine from 'lucide/dist/esm/icons/pen-line.mjs';
import LucidePencil from 'lucide/dist/esm/icons/pencil.mjs';
import LucidePlus from 'lucide/dist/esm/icons/plus.mjs';
import LucidePrinter from 'lucide/dist/esm/icons/printer.mjs';
import LucideRotateCw from 'lucide/dist/esm/icons/rotate-cw.mjs';
import LucideScanBarcode from 'lucide/dist/esm/icons/scan-barcode.mjs';
import LucideSearch from 'lucide/dist/esm/icons/search.mjs';
import LucideSettings from 'lucide/dist/esm/icons/settings.mjs';
import LucideShieldCheck from 'lucide/dist/esm/icons/shield-check.mjs';
import LucideShoppingCart from 'lucide/dist/esm/icons/shopping-cart.mjs';
import LucideSquare from 'lucide/dist/esm/icons/square.mjs';
import LucideSquareCheck from 'lucide/dist/esm/icons/square-check.mjs';
import LucideSquareKanban from 'lucide/dist/esm/icons/square-kanban.mjs';
import LucideStar from 'lucide/dist/esm/icons/star.mjs';
import LucideTrash from 'lucide/dist/esm/icons/trash.mjs';
import LucideTriangleAlert from 'lucide/dist/esm/icons/triangle-alert.mjs';
import LucideTruck from 'lucide/dist/esm/icons/truck.mjs';
import LucideWifiOff from 'lucide/dist/esm/icons/wifi-off.mjs';
import LucideX from 'lucide/dist/esm/icons/x.mjs';

/** Un elemento del dibujo de un ícono de Lucide: etiqueta SVG y sus atributos. */
export type NodoIcono = [string, Record<string, string | number>];

/** Catálogo de íconos de PolyConecta (spec 011, research R-07): un nombre por intención, no por dibujo. */
export const ICONOS = {
  nuevo: LucidePlus, editar: LucidePencil, quitar: LucideX, 'quitar-linea': LucideCircleX, borrar: LucideTrash,
  confirmar: LucideCircleCheck, validar: LucideCircleCheckBig, hecho: LucideCheck, cancelar: LucideCircleX, cerrar: LucideX,
  'hard-stop': LucideOctagonX, aviso: LucideCircleAlert, advertencia: LucideTriangleAlert, imprimir: LucidePrinter,
  excel: LucideFileSpreadsheet, descargar: LucideDownload, buscar: LucideSearch, filtro: LucideFunnel, agrupar: LucideGroup,
  favorito: LucideStar, columnas: LucideColumns3, lista: LucideList, kanban: LucideSquareKanban, anterior: LucideChevronLeft,
  siguiente: LucideChevronRight, expandir: LucideChevronRight, contraer: LucideChevronDown, regresar: LucideArrowLeft,
  acciones: LucideSettings, configuracion: LucideSettings, mas: LucideEllipsis, arrastrar: LucideGripVertical,
  pedido: LucideShoppingCart, fabricacion: LucideNetwork, orden: LucideSettings, calidad: LucideShieldCheck, entrega: LucideTruck,
  traslado: LucideTruck, recoleccion: LucideLogOut, recepcion: LucidePackageOpen, devolucion: LucideCornerDownRight,
  inventario: LucideBoxes, paquete: LucidePackage, lotes: LucideLayers, escanear: LucideScanBarcode, documento: LucideFileText,
  vinculo: LucideLink, casilla: LucideSquare, 'casilla-marcada': LucideSquareCheck, calendario: LucideCalendar,
  firma: LucidePenLine, archivar: LucideArchive, usuario: LucideCircleUser, notificaciones: LucideBell, aplicaciones: LucideLayoutGrid,
  'sin-conexion': LucideWifiOff, sincronizado: LucideCloudCheck, pendiente: LucideClock, reintentar: LucideRotateCw,
  cargando: LucideLoaderCircle,
} satisfies Record<string, NodoIcono[]>;

export type NombreIcono = keyof typeof ICONOS;

export type ContextoIcono = 'boton' | 'icono' | 'inteligente' | 'barra' | 'aplicacion';
const TAMANOS: Record<ContextoIcono, number> = { boton: 16, icono: 18, inteligente: 15, barra: 20, aplicacion: 34 };

/** El nombre del catálogo, o `undefined` si no existe. */
export function resolverIcono(nombre: string): NombreIcono | undefined {
  return nombre in ICONOS ? (nombre as NombreIcono) : undefined;
}

/**
 * Ícono de PolyConecta (spec 011): Lucide con trazo 2 y tamaño por contexto. Usa el paquete base `lucide`,
 * un módulo por ícono, y pinta el SVG aquí: así solo entran los íconos del catálogo. Es decorativo
 * (`aria-hidden`); el botón que lo contiene lleva el texto o el `aria-label`.
 */
@Component({
  selector: 'pc-odoo-icon',
  template: `
    @if (icono(); as nodos) {
      <svg xmlns="http://www.w3.org/2000/svg" [attr.width]="tamano()" [attr.height]="tamano()" viewBox="0 0 24 24" fill="none"
           stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"
           [class.o_icon_spin]="nombre() === 'cargando'">
        @for (n of nodos; track $index) {
          @switch (n[0]) {
            @case ('path') { <svg:path [attr.d]="n[1]['d']" /> }
            @case ('circle') { <svg:circle [attr.cx]="n[1]['cx']" [attr.cy]="n[1]['cy']" [attr.r]="n[1]['r']" /> }
            @case ('rect') { <svg:rect [attr.x]="n[1]['x']" [attr.y]="n[1]['y']" [attr.width]="n[1]['width']" [attr.height]="n[1]['height']" [attr.rx]="n[1]['rx']" [attr.ry]="n[1]['ry']" /> }
            @case ('line') { <svg:line [attr.x1]="n[1]['x1']" [attr.y1]="n[1]['y1']" [attr.x2]="n[1]['x2']" [attr.y2]="n[1]['y2']" /> }
            @case ('polyline') { <svg:polyline [attr.points]="n[1]['points']" /> }
            @case ('polygon') { <svg:polygon [attr.points]="n[1]['points']" /> }
            @case ('ellipse') { <svg:ellipse [attr.cx]="n[1]['cx']" [attr.cy]="n[1]['cy']" [attr.rx]="n[1]['rx']" [attr.ry]="n[1]['ry']" /> }
          }
        }
      </svg>
    }
  `,
  styles: ':host { display: inline-flex; align-items: center; line-height: 0; vertical-align: -0.15em; }',
})
export class OdooIcon {
  readonly nombre = input.required<string>();
  readonly contexto = input<ContextoIcono>('boton');
  protected readonly icono = computed(() => {
    const n = resolverIcono(this.nombre());
    return n ? ICONOS[n] : null;
  });
  protected readonly tamano = computed(() => TAMANOS[this.contexto()]);
}
