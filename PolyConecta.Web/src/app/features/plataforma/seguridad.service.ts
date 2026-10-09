import { Injectable } from '@angular/core';
import { pedirApi } from '../../core/sesion/api';

export interface PlantaItem {
  id: number;
  codigo: string;
  nombre: string;
}

export interface AgenteItem {
  id: number;
  codigo: string;
  nombre: string;
  erpId: string;
}

export interface AsignacionUsuarioItem {
  grupoId: number;
  grupo?: string;
  grupoCodigo?: string;
  plantaId: number;
  planta?: string;
  suplente: boolean;
}

export interface UsuarioDetalleDto {
  id: number;
  usuario: string;
  nombre: string;
  email: string | null;
  activo: boolean;
  agenteId: number | null;
  rowVersion: string;
  asignaciones: AsignacionUsuarioItem[];
}

export interface GrupoDetalleDto {
  id: number;
  codigo: string;
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  rowVersion: string;
  miembros: number;
  permisos: string[];
}

export interface AccionPermisoDto {
  clave: string;
  accion: string;
  etiqueta: string;
}

export interface ObjetoPermisoDto {
  objeto: string;
  etiqueta: string;
  tipo: string;
  acciones: AccionPermisoDto[];
}

export interface ModuloPermisoDto {
  modulo: string;
  etiqueta: string;
  objetos: ObjetoPermisoDto[];
}

@Injectable({ providedIn: 'root' })
export class SeguridadService {
  private peticion<T>(url: string, opciones?: RequestInit): Promise<T> {
    return pedirApi<T>(url, opciones);
  }

  // --- Plantas y Agentes ---
  async obtenerPlantas(): Promise<PlantaItem[]> {
    return this.peticion<PlantaItem[]>('/api/v1/plataforma/plantas');
  }

  async obtenerAgentes(): Promise<AgenteItem[]> {
    return this.peticion<AgenteItem[]>('/api/v1/ventas/agentes');
  }

  // --- Usuarios ---
  async obtenerUsuario(id: number | string): Promise<UsuarioDetalleDto> {
    return this.peticion<UsuarioDetalleDto>(`/api/v1/plataforma/usuarios/${id}`);
  }

  async crearUsuario(datos: {
    usuario: string;
    nombre: string;
    email?: string | null;
    contrasena: string;
    asignaciones: { grupoId: number; plantaId: number; suplente: boolean }[];
  }): Promise<UsuarioDetalleDto> {
    return this.peticion<UsuarioDetalleDto>('/api/v1/plataforma/usuarios', {
      method: 'POST',
      body: JSON.stringify(datos),
    });
  }

  async editarUsuario(
    id: number | string,
    datos: {
      rowVersion: string;
      nombre: string;
      email?: string | null;
      asignaciones: { grupoId: number; plantaId: number; suplente: boolean }[];
    }
  ): Promise<UsuarioDetalleDto> {
    return this.peticion<UsuarioDetalleDto>(`/api/v1/plataforma/usuarios/${id}`, {
      method: 'PUT',
      body: JSON.stringify(datos),
    });
  }

  async archivarUsuario(id: number | string): Promise<UsuarioDetalleDto> {
    return this.peticion<UsuarioDetalleDto>(`/api/v1/plataforma/usuarios/${id}/archivar`, {
      method: 'POST',
      body: JSON.stringify({}),
    });
  }

  async restaurarUsuario(id: number | string): Promise<UsuarioDetalleDto> {
    return this.peticion<UsuarioDetalleDto>(`/api/v1/plataforma/usuarios/${id}/restaurar`, {
      method: 'POST',
      body: JSON.stringify({}),
    });
  }

  async restablecerContrasena(id: number | string, contrasena: string): Promise<void> {
    return this.peticion<void>(`/api/v1/plataforma/usuarios/${id}/contrasena`, {
      method: 'POST',
      body: JSON.stringify({ contrasena }),
    });
  }

  async ligarAgente(id: number | string, agenteId: number | null): Promise<UsuarioDetalleDto> {
    return this.peticion<UsuarioDetalleDto>(`/api/v1/plataforma/usuarios/${id}/agente`, {
      method: 'PUT',
      body: JSON.stringify({ agenteId }),
    });
  }

  // --- Grupos ---
  async obtenerGrupo(id: number | string): Promise<GrupoDetalleDto> {
    return this.peticion<GrupoDetalleDto>(`/api/v1/plataforma/grupos/${id}`);
  }

  async crearGrupo(datos: {
    codigo: string;
    nombre: string;
    descripcion?: string | null;
    copiarDe?: number | null;
  }): Promise<GrupoDetalleDto> {
    return this.peticion<GrupoDetalleDto>('/api/v1/plataforma/grupos', {
      method: 'POST',
      body: JSON.stringify(datos),
    });
  }

  async editarGrupo(
    id: number | string,
    datos: {
      rowVersion: string;
      nombre: string;
      descripcion?: string | null;
      permisos: string[];
    }
  ): Promise<GrupoDetalleDto> {
    return this.peticion<GrupoDetalleDto>(`/api/v1/plataforma/grupos/${id}`, {
      method: 'PUT',
      body: JSON.stringify(datos),
    });
  }

  async archivarGrupo(id: number | string): Promise<GrupoDetalleDto> {
    return this.peticion<GrupoDetalleDto>(`/api/v1/plataforma/grupos/${id}/archivar`, {
      method: 'POST',
      body: JSON.stringify({}),
    });
  }

  async restaurarGrupo(id: number | string): Promise<GrupoDetalleDto> {
    return this.peticion<GrupoDetalleDto>(`/api/v1/plataforma/grupos/${id}/restaurar`, {
      method: 'POST',
      body: JSON.stringify({}),
    });
  }

  async obtenerPermisos(): Promise<ModuloPermisoDto[]> {
    return this.peticion<ModuloPermisoDto[]>('/api/v1/plataforma/permisos');
  }

  async listarGruposCatalogo(): Promise<{ id: number; codigo: string; nombre: string }[]> {
    const res = await this.peticion<{ completo: boolean; filas?: { id: number; codigo: string; nombre: string }[] }>(
      '/api/v1/plataforma/grupos/conjunto',
      {
        method: 'POST',
        body: JSON.stringify({}),
      }
    );
    return res.filas ?? [];
  }
}
