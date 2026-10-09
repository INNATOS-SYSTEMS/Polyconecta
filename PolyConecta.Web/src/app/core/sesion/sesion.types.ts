/**
 * Tipos de datos de la sesión del usuario (contracts/api-f1.md, Sesión).
 */

export interface UsuarioSesion {
  id: number;
  usuario: string;
  nombre: string;
}

export interface AsignacionSesion {
  grupo: string;
  nombreGrupo: string;
  planta: string;
  suplente: boolean;
}

export interface Sesion {
  usuario: UsuarioSesion;
  asignaciones: AsignacionSesion[];
  permisos: string[];
}

export interface EntrarRequest {
  usuario: string;
  contrasena: string;
}
