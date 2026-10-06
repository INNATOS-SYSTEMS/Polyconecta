/** Menús de la barra superior, iguales a los de Components/Shell/OdooTopbar.razor. */
export interface MenuItem {
  readonly label: string;
  readonly route: string;
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
      route: '/entregas',
      children: [
        { label: 'Entrega', route: '/entregas' },
        { label: 'Recolección', route: '/recolecciones' },
        { label: 'Traslado', route: '/traslados' },
        { label: 'Recepción', route: '/recepcion' },
      ],
    },
  ],
};

const VENTAS: ModuleInfo = {
  name: 'Ventas',
  items: [
    { label: 'Pedidos', route: '/pedidos' },
    { label: 'Inventario', route: '/ventas/inventario' },
  ],
};

const FABRICACION: ModuleInfo = {
  name: 'Fabricación',
  items: [
    { label: 'Fabricación', route: '/fabricacion' },
    { label: 'Producción', route: '/captura-masiva' },
    { label: 'Incidencias', route: '/incidencias' },
  ],
};

const CALIDAD: ModuleInfo = { name: 'Calidad', items: [{ label: 'Calidad', route: '/calidad' }] };

export const EMPTY_MODULE: ModuleInfo = { name: '', items: [] };

/** El primer prefijo que coincide gana, en el mismo orden que el prototipo. */
export const MODULE_MAP: readonly (readonly [string, ModuleInfo])[] = [
  ['/pedidos', VENTAS],
  ['/ventas', VENTAS],
  ['/inventario', INVENTARIO],
  ['/fabricacion', FABRICACION],
  ['/captura-masiva', FABRICACION],
  ['/incidencias', FABRICACION],
  ['/calidad', CALIDAD],
  ['/traslados', INVENTARIO],
  ['/recepcion', INVENTARIO],
  ['/recolecciones', INVENTARIO],
  ['/entregas', INVENTARIO],
];

export function moduleFor(path: string): ModuleInfo {
  const lower = path.toLowerCase();
  return MODULE_MAP.find(([prefix]) => lower.startsWith(prefix))?.[1] ?? EMPTY_MODULE;
}
