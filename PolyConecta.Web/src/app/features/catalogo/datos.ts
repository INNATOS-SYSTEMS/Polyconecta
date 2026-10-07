/**
 * Datos de ejemplo de la galería (spec 011). Están aparte de la semilla (`core/seed`), que usan los
 * guiones de escenario y no debe cambiar.
 */
export interface PedidoEjemplo {
  id: string;
  folio: string;
  cliente: string;
  estado: string;
  total: number;
  fecha: Date;
}

export interface ProductoEjemplo {
  clave: string;
  nombre: string;
  unidad: string;
}

const CLIENTES = ['EMPRESA MEXICANA DE MANUFACTURA', 'BOLSAS DEL NORTE', 'PLÁSTICOS SAN PEDRO', 'EMPAQUES MTY', 'DISTRIBUIDORA CUMBRES'];
const ESTADOS = ['Borrador', 'Confirmado', 'Autorizado', 'En progreso', 'Hecho'];

/** 57 pedidos: más de una página de 20 y varios grupos por cliente y estado. */
export function pedidosEjemplo(): PedidoEjemplo[] {
  return Array.from({ length: 57 }, (_, i) => ({
    id: `p${i + 1}`,
    folio: `PV-2026-${String(i + 1).padStart(4, '0')}`,
    cliente: CLIENTES[i % CLIENTES.length],
    estado: ESTADOS[(i * 7) % ESTADOS.length],
    total: 1000 + ((i * 3779) % 50000),
    fecha: new Date(2026, 9, 1 + (i % 28)),
  }));
}

/** 20 productos: más de los 8 que muestra la selección de registro, para probar "Buscar más…". */
export const PRODUCTOS_EJEMPLO: ProductoEjemplo[] = [
  { clave: 'PT1113 C567', nombre: 'BOLSA MEDIANA 44X84 C.430', unidad: 'PZA' },
  { clave: 'PT3413 C455', nombre: 'ROLLO EXTRUIDO NATURAL', unidad: 'KG' },
  { clave: 'PT3413 C4235', nombre: 'ROLLO IMPRESO 2 TINTAS', unidad: 'KG' },
  { clave: 'PT3413 C4236', nombre: 'BOLSA IMPRESA CAMISETA', unidad: 'MIL' },
  ...Array.from({ length: 12 }, (_, i) => ({ clave: `MP${String(i + 1).padStart(4, '0')}`, nombre: `RESINA ${['BAJA DENSIDAD', 'ALTA DENSIDAD', 'LINEAL'][i % 3]} LOTE ${i + 1}`, unidad: 'KG' })),
  { clave: 'PG0001', nombre: 'PIGMENTO BLANCO', unidad: 'KG' },
  { clave: 'PG0002', nombre: 'PIGMENTO NEGRO', unidad: 'KG' },
  { clave: 'TN0001', nombre: 'TINTA AZUL', unidad: 'LT' },
  { clave: 'TN0002', nombre: 'TINTA ROJA', unidad: 'LT' },
];
