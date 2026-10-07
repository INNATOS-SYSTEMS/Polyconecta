import { Component, computed, input } from '@angular/core';
import {
  LucideArchive, LucideArrowLeft, LucideBell, LucideBoxes, LucideCalendar, LucideCheck, LucideChevronDown,
  LucideChevronLeft, LucideChevronRight, LucideCircleAlert, LucideCircleCheck, LucideCircleCheckBig, LucideCircleUser,
  LucideCircleX, LucideClock, LucideCloudCheck, LucideColumns3, LucideCornerDownRight, LucideDownload, LucideDynamicIcon,
  LucideEllipsis, LucideFileSpreadsheet, LucideFileText, LucideFunnel, LucideGripVertical, LucideGroup, LucideLayers,
  LucideLayoutGrid, LucideLink, LucideList, LucideLoaderCircle, LucideLogOut, LucideNetwork, LucideOctagonX,
  LucidePackage, LucidePackageOpen, LucidePencil, LucidePenLine, LucidePlus, LucidePrinter, LucideRotateCw,
  LucideScanBarcode, LucideSearch, LucideSettings, LucideShieldCheck, LucideShoppingCart, LucideSquare, LucideSquareCheck,
  LucideSquareKanban, LucideStar, LucideTrash, LucideTriangleAlert, LucideTruck, LucideWifiOff, LucideX, type LucideIcon,
} from '@lucide/angular';

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
} satisfies Record<string, LucideIcon>;

export type NombreIcono = keyof typeof ICONOS;

/** Íconos de Bootstrap que la aplicación usaba, con su equivalente: permite migrar pantalla por pantalla. */
export const DE_BOOTSTRAP: Record<string, NombreIcono> = {
  'bi-x-circle-fill': 'quitar-linea', 'bi-gear-wide-connected': 'orden', 'bi-x-octagon': 'hard-stop', 'bi-x-lg': 'cerrar',
  'bi-list-ul': 'lista', 'bi-check-circle': 'confirmar', 'bi-truck': 'entrega', 'bi-plus-lg': 'nuevo', 'bi-pencil': 'editar',
  'bi-chevron-right': 'siguiente', 'bi-x-circle': 'cancelar', 'bi-x': 'quitar', 'bi-search': 'buscar', 'bi-exclamation-circle': 'aviso',
  'bi-cart-check': 'pedido', 'bi-upc-scan': 'escanear', 'bi-square': 'casilla', 'bi-shield-check': 'calidad', 'bi-printer': 'imprimir',
  'bi-file-earmark-excel': 'excel', 'bi-exclamation-triangle': 'advertencia', 'bi-diagram-3': 'fabricacion', 'bi-check2-circle': 'validar',
  'bi-check-square-fill': 'casilla-marcada', 'bi-box-arrow-right': 'recoleccion', 'bi-box-arrow-in-down': 'recepcion',
  'bi-arrow-return-right': 'devolucion', 'bi-wifi-off': 'sin-conexion', 'bi-stack': 'lotes', 'bi-person-circle': 'usuario',
  'bi-link-45deg': 'vinculo', 'bi-kanban': 'kanban', 'bi-grid-3x3-gap-fill': 'aplicaciones', 'bi-gear': 'configuracion',
  'bi-funnel': 'filtro', 'bi-file-text': 'documento', 'bi-cloud-check': 'sincronizado', 'bi-chevron-left': 'anterior',
  'bi-check-lg': 'hecho', 'bi-cart-check-fill': 'pedido', 'bi-caret-right-fill': 'expandir', 'bi-caret-down-fill': 'contraer',
  'bi-boxes': 'inventario', 'bi-box-seam': 'paquete', 'bi-bell': 'notificaciones',
};

export type ContextoIcono = 'boton' | 'icono' | 'inteligente' | 'barra';
const TAMANOS: Record<ContextoIcono, number> = { boton: 16, icono: 18, inteligente: 15, barra: 20 };

/** Resuelve un nombre del catálogo, o uno de Bootstrap (`bi bi-truck`, `bi-truck`), a su nombre del catálogo. */
export function resolverIcono(nombre: string): NombreIcono | undefined {
  if (nombre in ICONOS) return nombre as NombreIcono;
  const bi = nombre.split(/\s+/).find(c => c.startsWith('bi-'));
  return bi ? DE_BOOTSTRAP[bi] : undefined;
}

/**
 * Ícono de PolyConecta (spec 011): Lucide con trazo 2 y tamaño por contexto. Es decorativo
 * (`aria-hidden`); el botón que lo contiene lleva el texto o el `aria-label`.
 */
@Component({
  selector: 'pc-odoo-icon',
  imports: [LucideDynamicIcon],
  template: `
    @if (icono(); as i) {
      <svg [lucideIcon]="i" [size]="tamano()" [strokeWidth]="2" aria-hidden="true" [class.o_icon_spin]="nombre() === 'cargando'"></svg>
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
