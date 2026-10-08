import { InventoryState } from '../state/inventory-state';
import { nombreProducto } from '../format/producto';
import { ProductClass, StockQuant } from '../models/inventario';
import { StockOperation } from '../models/operaciones';
import { Incidencia, ManufacturingOrder } from '../models/produccion';
import { SearchFilter, SearchView } from './search-view';

/**
 * Registro central de vistas de búsqueda (Services/SearchViews.cs). Cambiar aquí los campos o filtros
 * de un modelo no requiere tocar su pantalla.
 */
const sinMayusculas = (texto: string, prefijo: string) => texto.toLowerCase().startsWith(prefijo.toLowerCase());
const mismoDia = (a: Date, b: Date) => a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
const soloFecha = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate());

export const FABRICACION: SearchView<ManufacturingOrder> = {
  referencia: o => o.folio,
  campos: [
    { etiqueta: 'Folio', valor: o => o.folio },
    { etiqueta: 'Producto', valor: o => o.producto },
    { etiqueta: 'Proceso', valor: o => o.processLabel },
    { etiqueta: 'Pedido', valor: o => o.pedidoFolio },
  ],
  filtros: [
    { nombre: 'Borrador', campo: 'Estado', condicion: o => o.state === 'Borrador' },
    { nombre: 'Planeado', campo: 'Estado', condicion: o => o.state === 'Planeado' },
    { nombre: 'En progreso', campo: 'Estado', condicion: o => o.state === 'En progreso' },
    { nombre: 'Hecho', campo: 'Estado', condicion: o => o.state === 'Hecho' },
    { nombre: 'Extrusión', campo: 'Proceso', condicion: o => o.processType === 'Extrusion' },
    { nombre: 'Impresión', campo: 'Proceso', condicion: o => o.processType === 'Impresion' },
    { nombre: 'Bolseo', campo: 'Proceso', condicion: o => o.processType === 'Bolseo' },
    { nombre: 'Órdenes maestras', campo: 'Jerarquía', condicion: o => o.originFolio === undefined },
  ],
  agrupaciones: [
    { etiqueta: 'Estado', clave: o => o.state },
    { etiqueta: 'Proceso', clave: o => o.processLabel },
    { etiqueta: 'Pedido', clave: o => o.pedidoFolio },
  ],
};

export const OPERACIONES: SearchView<StockOperation> = {
  referencia: o => o.folio,
  campos: [
    { etiqueta: 'Folio', valor: o => o.folio },
    { etiqueta: 'Operación', valor: o => o.operacion },
    { etiqueta: 'Orden', valor: o => o.ofFolio },
    { etiqueta: 'Origen', valor: o => o.origen },
    { etiqueta: 'Destino', valor: o => o.destino },
  ],
  filtros: [
    { nombre: 'Borrador', campo: 'Estado', condicion: o => o.state === 'Borrador' },
    { nombre: 'En espera', campo: 'Estado', condicion: o => o.state === 'En espera' },
    { nombre: 'Listo', campo: 'Estado', condicion: o => o.state === 'Listo' },
    { nombre: 'Hecho', campo: 'Estado', condicion: o => o.state === 'Hecho' },
    { nombre: 'Recolecciones', campo: 'Tipo', condicion: o => !o.tipo.esDevolucion },
    { nombre: 'Devoluciones', campo: 'Tipo', condicion: o => o.tipo.esDevolucion },
    { nombre: 'Backorders', campo: 'Origen del documento', condicion: o => o.backorderDe !== undefined },
    { nombre: 'Pendientes de surtir', campo: 'Avance', condicion: o => o.esParcial },
  ],
  agrupaciones: [
    { etiqueta: 'Estado', clave: o => o.state },
    { etiqueta: 'Tipo de operación', clave: o => o.tipo.nombre },
    { etiqueta: 'Orden de fabricación', clave: o => o.ofFolio },
  ],
};

/** Proyección ligera de la lista de pedidos. */
export interface SalesOrderRow {
  folio: string;
  cliente: string;
  producto: string;
  estado: string;
}

export const PEDIDOS: SearchView<SalesOrderRow> = {
  referencia: p => p.folio,
  campos: [
    { etiqueta: 'Folio', valor: p => p.folio },
    { etiqueta: 'Cliente', valor: p => p.cliente },
    { etiqueta: 'Producto', valor: p => p.producto },
  ],
  filtros: ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'].map(e => ({ nombre: e, campo: 'Estado', condicion: (p: SalesOrderRow) => p.estado === e })),
  agrupaciones: [
    { etiqueta: 'Estado', clave: p => p.estado },
    { etiqueta: 'Cliente', clave: p => p.cliente },
  ],
};

/** Control de calidad: se lista sobre las órdenes que lo requieren. */
export const CALIDAD: SearchView<ManufacturingOrder> = {
  referencia: o => o.folio,
  campos: [
    { etiqueta: 'Orden', valor: o => o.folio },
    { etiqueta: 'Producto', valor: o => o.producto },
    { etiqueta: 'Proceso', valor: o => o.processLabel },
  ],
  filtros: [
    { nombre: 'Con lotes en revisión', campo: 'Resultado', condicion: o => o.produccion.some(l => l.estado === 'En revisión') },
    { nombre: 'Totalmente aprobados', campo: 'Resultado', condicion: o => o.produccion.length > 0 && o.produccion.every(l => l.estado === 'Aprobado') },
    { nombre: 'Con rechazos', campo: 'Resultado', condicion: o => o.produccion.some(l => l.estado === 'Rechazado') },
    { nombre: 'Sin producción capturada', campo: 'Avance', condicion: o => o.produccion.length === 0 },
    { nombre: 'Extrusión', campo: 'Proceso', condicion: o => o.processType === 'Extrusion' },
    { nombre: 'Impresión', campo: 'Proceso', condicion: o => o.processType === 'Impresion' },
    { nombre: 'Bolseo', campo: 'Proceso', condicion: o => o.processType === 'Bolseo' },
  ],
  agrupaciones: [
    { etiqueta: 'Proceso', clave: o => o.processLabel },
    { etiqueta: 'Estado', clave: o => o.state },
  ],
};

/** Proyección común de las operaciones logísticas de documento único. */
export interface LogisticsRow {
  folio: string;
  operacion: string;
  origen: string;
  destino: string;
  estado: string;
}

const logistica = (...estados: string[]): SearchView<LogisticsRow> => ({
  referencia: l => l.folio,
  campos: [
    { etiqueta: 'Folio', valor: l => l.folio },
    { etiqueta: 'Operación', valor: l => l.operacion },
    { etiqueta: 'Origen', valor: l => l.origen },
    { etiqueta: 'Destino', valor: l => l.destino },
  ],
  filtros: estados.map(e => ({ nombre: e, campo: 'Estado', condicion: (l: LogisticsRow) => l.estado === e })),
  agrupaciones: [
    { etiqueta: 'Estado', clave: l => l.estado },
    { etiqueta: 'Destino', clave: l => l.destino },
  ],
});

export const TRASLADOS = logistica('Borrador', 'En espera de operación', 'En espera', 'Listo', 'Hecho');
export const RECEPCIONES = logistica('Borrador', 'En espera', 'Listo', 'Hecho');
export const ENTREGAS = logistica('Borrador', 'En espera', 'Listo', 'Hecho');

export const INCIDENCIAS: SearchView<Incidencia> = {
  referencia: i => i.centroTrabajo,
  campos: [
    { etiqueta: 'Centro de trabajo', valor: i => i.centroTrabajo },
    { etiqueta: 'Tipo', valor: i => i.tipo },
    { etiqueta: 'Comentarios', valor: i => i.comentarios },
  ],
  filtros: [
    { nombre: 'Extrusión', campo: 'Centro', condicion: i => sinMayusculas(i.centroTrabajo, 'EXT') || sinMayusculas(i.centroTrabajo, 'COEXT') },
    { nombre: 'Impresión', campo: 'Centro', condicion: i => sinMayusculas(i.centroTrabajo, 'IMP') },
    { nombre: 'Bolseo', campo: 'Centro', condicion: i => sinMayusculas(i.centroTrabajo, 'BOL') },
    { nombre: 'Hoy', campo: 'Fecha', condicion: i => mismoDia(i.fecha, new Date(2026, 8, 23)) },
    { nombre: 'Esta semana', campo: 'Fecha', condicion: i => soloFecha(i.fecha) >= new Date(2026, 8, 21) },
  ],
  agrupaciones: [
    { etiqueta: 'Centro de trabajo', clave: i => i.centroTrabajo },
    { etiqueta: 'Tipo', clave: i => i.tipo },
  ],
};

const CLASES: ProductClass[] = ['Bolsa', 'RolloImpreso', 'RolloLiso', 'RolloMaestro', 'MateriaPrima', 'Scrap'];

/** Inventario Actual: existencias por lote, agrupadas por ubicación y producto. */
export const INVENTARIO_ACTUAL: SearchView<StockQuant> = {
  referencia: q => q.producto.clave,
  campos: [
    { etiqueta: 'Producto', valor: q => q.producto.clave + ' ' + q.producto.nombre },
    { etiqueta: 'Ubicación', valor: q => q.ubicacion },
    { etiqueta: 'Lote', valor: q => q.lote },
  ],
  filtros: [
    ...CLASES.map((c): SearchFilter<StockQuant> => ({ nombre: InventoryState.classLabel(c), campo: 'Clasificación', condicion: q => q.producto.clasificacion === c })),
    { nombre: 'PIM', campo: 'Planta', condicion: q => sinMayusculas(q.ubicacion, 'PIM') },
    { nombre: 'Santa Cruz', campo: 'Planta', condicion: q => sinMayusculas(q.ubicacion, 'SC') },
  ],
  agrupaciones: [
    { etiqueta: 'Ubicación', clave: q => q.ubicacion },
    { etiqueta: 'Producto', clave: q => nombreProducto(q.producto.clave, q.producto.nombre) },
    { etiqueta: 'Clasificación', clave: q => InventoryState.classLabel(q.producto.clasificacion) },
    { etiqueta: 'Lote', clave: q => q.lote },
  ],
  agrupacionesPorDefecto: ['Ubicación', 'Producto'],
};
