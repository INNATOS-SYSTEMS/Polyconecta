/**
 * Tipos de datos para el selector dual en árbol pc-odoo-dual-list (D-148, CT-24).
 */

export interface AccionPermisoItem {
  clave: string;
  accion: string;
  etiqueta: string;
}

export interface ObjetoPermisoItem {
  objeto: string;
  etiqueta: string;
  tipo?: string;
  acciones: AccionPermisoItem[];
}

export interface ModuloPermisoItem {
  modulo: string;
  etiqueta: string;
  objetos: ObjetoPermisoItem[];
}
