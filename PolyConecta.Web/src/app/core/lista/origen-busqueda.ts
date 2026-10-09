import { ConsultaLista, OrigenDeLista, ResultadoLista } from './origen';

/**
 * Origen para un many2one sobre una búsqueda de la API (`…/buscar?texto=`): cada consulta busca en el
 * servidor con el texto escrito, así el campo encuentra cualquier registro y no solo los primeros que se
 * cargaron al abrir la pantalla.
 */
export class OrigenBusqueda<T> implements OrigenDeLista<T> {
  constructor(private readonly buscar: (texto: string) => Promise<T[]>) {}

  async consultar(c: ConsultaLista): Promise<ResultadoLista<T>> {
    const encontrados = await this.buscar(c.busqueda?.trim() ?? '');
    return { filas: encontrados.slice(0, c.tamano), grupos: null, total: encontrados.length, totales: {} };
  }
}
