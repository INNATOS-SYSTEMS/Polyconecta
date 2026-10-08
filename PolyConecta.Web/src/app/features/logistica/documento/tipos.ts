import { DocumentoLogistica } from '../../../core/models/logistica';
import { ProductionLot } from '../../../core/models/produccion';
import { SearchView } from '../../../core/search/search-view';
import { ENTREGAS, LogisticsRow, RECEPCIONES, TRASLADOS } from '../../../core/search/views';
import { OperationalFlowState } from '../../../core/state/operational-flow-state';
import { SmartButtonModel, botonInteligente } from '../../../shared/odoo-smart-buttons/odoo-smart-buttons';
import { PEDIDO_FOLIO } from '../../../core/seed/flujo';

export type TipoLogistica = 'traslado' | 'recepcion' | 'entrega';

/**
 * Lo que distingue a Traslados, Recepción y Entregas en el prototipo (sus .razor son el mismo
 * marcado con otros textos, etapas y acciones).
 */
export interface ConfigLogistica {
  ruta: string;
  tituloLista: string;
  tituloForm: string;
  vista: SearchView<LogisticsRow>;
  mostrarOrigenEnLista: boolean;
  stages: string[];
  /** La entrega muestra "Cliente: " antes del destino. */
  prefijoDestino: string;
  contpaqPendiente: string;
  columnaCantidad: string;
  imprimir: boolean;
  chatter: string;
  documentos(flow: OperationalFlowState): DocumentoLogistica[];
  documento(flow: OperationalFlowState, folio?: string): DocumentoLogistica;
  validar(flow: OperationalFlowState, folio: string): void;
  comprobar(flow: OperationalFlowState): void;
  lotes(flow: OperationalFlowState): ProductionLot[];
  smartButtons(flow: OperationalFlowState): SmartButtonModel[];
}

export const CONFIG: Record<TipoLogistica, ConfigLogistica> = {
  traslado: {
    ruta: '/traslados',
    tituloLista: 'Traslados',
    tituloForm: 'Orden de traslado',
    vista: TRASLADOS,
    mostrarOrigenEnLista: true,
    stages: ['Borrador', 'En espera de operación', 'En espera', 'Listo', 'Hecho'],
    prefijoDestino: '',
    contpaqPendiente: '— (pendiente de recepción)',
    columnaCantidad: 'Cant. Entregada',
    imprimir: true,
    chatter: 'Traslado generado.',
    documentos: f => f.traslados,
    documento: (f, folio) => f.traslado(folio),
    validar: (f, folio) => f.validarTraslado(folio),
    comprobar: f => f.comprobarDisponibilidadTraslado(),
    lotes: f => f.getLotesDisponiblesTraslado(),
    smartButtons: f => [
      botonInteligente('orden', 1, '/fabricacion/IMP-2026-0001'),
      botonInteligente('recepcion', 1, `/recepcion/${f.recepcion().folio}`),
    ],
  },
  recepcion: {
    ruta: '/recepcion',
    tituloLista: 'Recepción',
    tituloForm: 'Recepción',
    vista: RECEPCIONES,
    mostrarOrigenEnLista: true,
    stages: ['Borrador', 'En espera', 'Listo', 'Hecho'],
    prefijoDestino: '',
    contpaqPendiente: '— (pendiente)',
    columnaCantidad: 'Cant. Recibida',
    imprimir: true,
    chatter: 'Recepción generada al validar el Traslado.',
    documentos: f => f.recepciones,
    documento: (f, folio) => f.recepcion(folio),
    validar: (f, folio) => f.validarRecepcion(folio),
    comprobar: f => f.comprobarDisponibilidadRecepcion(),
    lotes: f => f.getLotesDisponiblesTraslado(),
    smartButtons: f => [
      botonInteligente('traslado', 1, `/traslados/${f.traslado().folio}`),
      botonInteligente('orden', 1, '/fabricacion/IMP-2026-0001'),
    ],
  },
  entrega: {
    ruta: '/entregas',
    tituloLista: 'Entregas',
    tituloForm: 'Orden de entrega',
    vista: ENTREGAS,
    mostrarOrigenEnLista: false,
    stages: ['Borrador', 'En espera', 'Listo', 'Hecho'],
    prefijoDestino: 'Cliente: ',
    contpaqPendiente: '— (Remisión pendiente)',
    columnaCantidad: 'Cant. Entregada',
    imprimir: false,
    chatter: 'Entrega generada.',
    documentos: f => f.entregas,
    documento: (f, folio) => f.entrega(folio),
    validar: (f, folio) => f.validarEntrega(folio),
    comprobar: f => f.comprobarDisponibilidadEntrega(),
    lotes: f => f.getLotesDisponiblesEntrega(),
    smartButtons: () => [botonInteligente('pedido', 1, `/pedidos/${PEDIDO_FOLIO}`)],
  },
};
