import { Injectable } from '@angular/core';

export interface DatosRollo {
  materialType: string;
  rollTypeSize: string;
  gaugeMicrons?: number | null;
  kgPerRoll?: number | null;
  treatmentDynes?: number | null;
  pigment?: string | null;
  additive?: string | null;
  perforation?: string | null;
  preliminaryPrint?: string | null;
}

export interface DatosPt {
  customerPartNumber?: string | null;
  finalSize?: string | null;
  inks?: string | null;
  pantones?: string | null;
  dieCut?: string | null;
  packaging?: string | null;
  sealType?: string | null;
  kgPerThousand?: number | null;
}

export interface ClasificacionDto {
  id: number;
  codigo: string;
  nombre: string;
}

export interface ProductoDetalleDto {
  id: number;
  codigo: string;
  nombre: string;
  unidadBase: string;
  controlaLote: boolean;
  clasificacionId: number | null;
  clasificacion: string | null;
  activo: boolean;
  rollo: DatosRollo | null;
  pt: DatosPt | null;
  rolloLigadoProductoId: number | null;
  rowVersion: string;
}

export interface DomicilioDto {
  id: number;
  tipo: string;
  texto?: string;
  calle?: string | null;
  numeroExterior?: string | null;
  numeroInterior?: string | null;
  colonia?: string | null;
  codigoPostal?: string | null;
  ciudad?: string | null;
  municipio?: string | null;
  estado?: string | null;
  pais?: string | null;
  sucursal?: string | null;
}

export interface ClienteDetalleDto {
  id: number;
  clave?: string;
  codigo?: string;
  razonSocial: string;
  etiqueta?: string;
  moneda?: string | null;
  rfc?: string | null;
  activo: boolean;
  domicilios?: DomicilioDto[];
  domicilioFiscal?: DomicilioDto | null;
  domiciliosEnvio?: DomicilioDto[];
}

export interface EstadoCatalogoDto {
  catalogo: string;
  ultimaCorrida: string | null;
  ultimaExitosa: string | null;
  resultado: string | null;
  leidos: number;
  cambiados: number;
  archivados: number;
  duracionMs: number;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class CatalogosService {
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
      try {
        const err = await res.json();
        if (err.detail) mensaje = err.detail;
        else if (err.title) mensaje = err.title;
        else if (err.error) mensaje = err.error;
      } catch {
        // Ignorar error al parsear JSON
      }
      throw new Error(mensaje);
    }

    if (res.status === 204) {
      return undefined as unknown as T;
    }

    return (await res.json()) as T;
  }

  // --- Productos e Inventario ---
  async obtenerProducto(id: number | string): Promise<ProductoDetalleDto> {
    return this.peticion<ProductoDetalleDto>(`/api/v1/inventario/productos/${id}`);
  }

  async clasificarProducto(id: number | string, clasificacionId: number | null): Promise<ProductoDetalleDto> {
    return this.peticion<ProductoDetalleDto>(`/api/v1/inventario/productos/${id}/clasificacion`, {
      method: 'PUT',
      body: JSON.stringify({ clasificacionId }),
    });
  }

  async guardarFichaTecnica(
    id: number | string,
    datos: { rollo: DatosRollo; pt: DatosPt; rolloLigadoProductoId?: number | null }
  ): Promise<ProductoDetalleDto> {
    return this.peticion<ProductoDetalleDto>(`/api/v1/inventario/productos/${id}/ficha-tecnica`, {
      method: 'PUT',
      body: JSON.stringify(datos),
    });
  }

  async listarClasificaciones(): Promise<ClasificacionDto[]> {
    return this.peticion<ClasificacionDto[]>('/api/v1/inventario/clasificaciones');
  }

  // --- Clientes y Ventas ---
  async obtenerCliente(id: number | string): Promise<ClienteDetalleDto> {
    return this.peticion<ClienteDetalleDto>(`/api/v1/ventas/clientes/${id}`);
  }

  // --- Sincronización ---
  async obtenerEstadoSincronizacion(): Promise<EstadoCatalogoDto[]> {
    return this.peticion<EstadoCatalogoDto[]>('/api/v1/plataforma/sincronizacion');
  }

  async sincronizarTodo(): Promise<EstadoCatalogoDto[]> {
    return this.peticion<EstadoCatalogoDto[]>('/api/v1/plataforma/sincronizacion', {
      method: 'POST',
      body: JSON.stringify({}),
    });
  }

  async sincronizarCatalogo(catalogo: string): Promise<EstadoCatalogoDto> {
    return this.peticion<EstadoCatalogoDto>(`/api/v1/plataforma/sincronizacion/${catalogo}`, {
      method: 'POST',
      body: JSON.stringify({}),
    });
  }
}
