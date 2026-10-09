/** Menús de la barra superior, iguales a los de Components/Shell/OdooTopbar.razor. */
export interface MenuItem {
  readonly label: string;
  readonly route: string;
  readonly permiso?: string;
  readonly children?: readonly MenuItem[];
}

export interface ModuleInfo {
  readonly name: string;
  readonly items: readonly MenuItem[];
}

/** Inventario absorbe lo que era Logística: las operaciones de almacén cuelgan de "Operaciones". */
const INVENTARIO: ModuleInfo = {
  name: 'Inventario',
  items: [
    { label: 'Inventario', route: '/inventario', children: [{ label: 'Inventario Actual', route: '/inventario' }] },
    {
      label: 'Operaciones',
      route: '/logistica/entregas',
      children: [
        { label: 'Entrega', route: '/logistica/entregas' },
        { label: 'Recolección', route: '/logistica/recolecciones' },
        { label: 'Traslado', route: '/logistica/traslados' },
        { label: 'Recepción', route: '/logistica/recepcion' },
      ],
    },
  ],
};

const VENTAS: ModuleInfo = {
  name: 'Ventas',
  items: [
    { label: 'Pedidos', route: '/ventas/pedidos' },
    { label: 'Inventario', route: '/ventas/inventario' },
  ],
};

const FABRICACION: ModuleInfo = {
  name: 'Fabricación',
  items: [
    { label: 'Fabricación', route: '/produccion/fabricacion' },
    { label: 'Producción', route: '/produccion/captura-masiva' },
    { label: 'Incidencias', route: '/produccion/incidencias' },
  ],
};

const CALIDAD: ModuleInfo = { name: 'Calidad', items: [{ label: 'Calidad', route: '/calidad' }] };

const CONFIGURACION: ModuleInfo = {
  name: 'Configuración',
  items: [
    { label: 'Usuarios', route: '/plataforma/usuarios', permiso: 'plataforma.usuarios.leer' },
    { label: 'Grupos', route: '/plataforma/grupos', permiso: 'plataforma.grupos.leer' },
  ],
};

export const EMPTY_MODULE: ModuleInfo = { name: '', items: [] };

/** El primer prefijo que coincide gana. Cada módulo de CT-09 tiene su prefijo (D-155); Logística cuelga del menú de Inventario. */
export const MODULE_MAP: readonly (readonly [string, ModuleInfo])[] = [
  ['/ventas', VENTAS],
  ['/inventario', INVENTARIO],
  ['/produccion', FABRICACION],
  ['/calidad', CALIDAD],
  ['/logistica', INVENTARIO],
  ['/plataforma', CONFIGURACION],
];

export function moduleFor(path: string): ModuleInfo {
  const lower = path.toLowerCase();
  return MODULE_MAP.find(([prefix]) => lower.startsWith(prefix))?.[1] ?? EMPTY_MODULE;
}
