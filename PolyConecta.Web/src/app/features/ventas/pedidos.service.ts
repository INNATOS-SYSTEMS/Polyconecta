import { Injectable } from '@angular/core';

export interface ReferenciaDto {
  id: number;
  clave: string;
  nombre: string;
}

export interface LineaPedidoInputDto {
  id?: number | null;
  productoId: number;
  cantidad: number;
  precioUnitario?: number | null;
  metaProduccionKg?: number | null;
  toleranciaPorcentaje?: number | null;
}

export interface DatosPedidoInputDto {
  clienteId: number;
  ordenCompraCliente?: string | null;
  agenteId?: number | null;
  fechaPedido?: string | null;
  fechaPromesa?: string | null;
  domicilioEntregaId?: number | null;
  moneda?: string | null;
  tipoCambio?: number | null;
  lineas?: LineaPedidoInputDto[];
}

export interface EditarPedidoInputDto extends DatosPedidoInputDto {
  rowVersion: string;
  revocarAutorizacion?: boolean;
}

export interface LineaDetalleDto {
  id: number;
  productoId: number;
  clave: string;
  producto: string;
  cantidad: number;
  unidad: string;
  precioUnitario: number | null;
  subtotal: number | null;
  metaProduccionKg: number | null;
  toleranciaPorcentaje: number | null;
}

export interface FirmaDetalleDto {
  rol: string;
  usuario: string;
  usuarioId: number;
  grupo?: string | null;
  suplente: boolean;
  fecha: string;
}

export interface AccionDisponibleDto {
  accion: string;
  disponible: boolean;
  razon?: string | null;
  aviso?: string | null;
}

export interface DomicilioEnvioDto {
  id: number;
  texto: string;
}

export interface ClienteBusquedaDto {
  id: number;
  clave: string;
  nombre: string;
  etiqueta: string;
  moneda?: string | null;
  domiciliosEnvio: DomicilioEnvioDto[];
}

export interface ProductoBusquedaDto {
  id: number;
  clave: string;
  nombre: string;
  etiqueta: string;
  unidad: string;
  unidadId: number;
  llevaLote: boolean;
}

export interface AgenteVentaDto {
  id: number;
  clave: string;
  nombre: string;
  etiqueta: string;
  tipo: string;
}

export interface PedidoDetalleDto {
  id: number;
  folio: string;
  estado: string; // Borrador, Confirmado, Autorizado, Cancelado
  rowVersion: string;
  cliente: ReferenciaDto;
  ordenCompraCliente?: string | null;
  agente?: ReferenciaDto | null;
  fechaPedido: string;
  fechaPromesa?: string | null;
  moneda: string;
  tipoCambio?: number | null;
  monedaBase: string;
  domicilioEntrega?: DomicilioEnvioDto | null;
  lineas: LineaDetalleDto[];
  firmas: FirmaDetalleDto[];
  firmasPendientes: string[];
  rolesPorFirmar: string[];
  sincronizacion?: unknown;
  acciones: AccionDisponibleDto[];
}

export class ErrorApi extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
    public readonly errores?: Array<{ campo: string; mensaje: string }>
  ) {
    super(message);
    this.name = 'ErrorApi';
  }
}

@Injectable({ providedIn: 'root' })
export class PedidosService {
  private async peticion<T>(url: string, opciones?: RequestInit): Promise<T> {
    const res = await fetch(url, {
      ...opciones,
      headers: {
        'Content-Type': 'application/json',
        'X-Requested-With': 'PolyConecta',
        ...(opciones?.headers ?? {}),
      },
      credentials: 'same-origin',
    });

    if (!res.ok) {
      let mensaje = `Error HTTP ${res.status}`;
      let codigo: string | undefined;
      let errores: Array<{ campo: string; mensaje: string }> | undefined;
      try {
        const err = await res.json();
        codigo = err.code ?? err.title;
        errores = err.errores;
        if (err.detail) mensaje = err.detail;
        else if (err.title) mensaje = err.title;
        else if (err.error) mensaje = err.error;
      } catch {
        // Ignorar fallo de parseo JSON
      }
      throw new ErrorApi(mensaje, res.status, codigo, errores);
    }

    if (res.status === 204) {
      return undefined as unknown as T;
    }

    return (await res.json()) as T;
  }

  async obtener(id: number | string): Promise<PedidoDetalleDto> {
    return this.peticion<PedidoDetalleDto>(`/api/v1/ventas/pedidos/${id}`);
  }

  async crear(datos: DatosPedidoInputDto): Promise<PedidoDetalleDto> {
    return this.peticion<PedidoDetalleDto>('/api/v1/ventas/pedidos', {
      method: 'POST',
      body: JSON.stringify(datos),
    });
  }

  async editar(id: number | string, datos: EditarPedidoInputDto): Promise<PedidoDetalleDto> {
    return this.peticion<PedidoDetalleDto>(`/api/v1/ventas/pedidos/${id}`, {
      method: 'PUT',
      body: JSON.stringify(datos),
    });
  }

  async confirmar(id: number | string, rowVersion: string): Promise<PedidoDetalleDto> {
    return this.peticion<PedidoDetalleDto>(`/api/v1/ventas/pedidos/${id}/confirmar`, {
      method: 'POST',
      body: JSON.stringify({ rowVersion }),
    });
  }

  async autorizar(id: number | string, rowVersion: string, rol?: string | null): Promise<PedidoDetalleDto> {
    return this.peticion<PedidoDetalleDto>(`/api/v1/ventas/pedidos/${id}/autorizar`, {
      method: 'POST',
      body: JSON.stringify({ rowVersion, rol: rol || null }),
    });
  }

  async revocar(id: number | string, rowVersion: string, motivo?: string | null): Promise<PedidoDetalleDto> {
    return this.peticion<PedidoDetalleDto>(`/api/v1/ventas/pedidos/${id}/revocar`, {
      method: 'POST',
      body: JSON.stringify({ rowVersion, motivo: motivo || null }),
    });
  }

  async buscarClientes(texto = ''): Promise<ClienteBusquedaDto[]> {
    return this.peticion<ClienteBusquedaDto[]>(
      `/api/v1/ventas/clientes/buscar?texto=${encodeURIComponent(texto)}`
    );
  }

  async buscarProductos(texto = ''): Promise<ProductoBusquedaDto[]> {
    return this.peticion<ProductoBusquedaDto[]>(
      `/api/v1/inventario/productos/buscar?texto=${encodeURIComponent(texto)}`
    );
  }

  async listarAgentes(): Promise<AgenteVentaDto[]> {
    return this.peticion<AgenteVentaDto[]>('/api/v1/ventas/agentes');
  }

  async cancelar(id: number | string, rowVersion: string, motivo?: string | null): Promise<PedidoDetalleDto> {
    return this.peticion<PedidoDetalleDto>(`/api/v1/ventas/pedidos/${id}/cancelar`, {
      method: 'POST',
      body: JSON.stringify({ rowVersion, motivo: motivo || null }),
    });
  }
}
