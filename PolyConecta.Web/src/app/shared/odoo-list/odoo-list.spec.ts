import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { ConsultaLista, OrigenDeLista, ResultadoLista } from '../../core/lista/origen';
import { OdooList } from './odoo-list';

interface Fila { id: string; nombre: string; total: number }

/** Origen espía: devuelve siempre lo mismo y guarda las consultas, para probar que la tabla no procesa por su cuenta. */
class OrigenEspia implements OrigenDeLista<Fila> {
  consultas: ConsultaLista[] = [];
  constructor(private readonly filas: Fila[]) {}
  async consultar(c: ConsultaLista): Promise<ResultadoLista<Fila>> {
    this.consultas.push(c);
    return { filas: this.filas, grupos: null, total: 100, totales: { total: 999 } };
  }
}

async function montar(origen: OrigenEspia) {
  localStorage.clear();
  const f = TestBed.createComponent(OdooList<Fila>);
  f.componentRef.setInput('lista', 'prueba');
  f.componentRef.setInput('origen', origen);
  f.componentRef.setInput('idDe', (x: Fila) => x.id);
  f.componentRef.setInput('columnas', [{ campo: 'nombre', titulo: 'Nombre' }, { campo: 'total', titulo: 'Total', tipo: 'numero', sumable: true }]);
  f.componentRef.setInput('tamanoInicial', 20);
  f.detectChanges();
  await f.whenStable();
  f.detectChanges();
  return f;
}

describe('OdooList', () => {
  it('pinta lo que devuelve el origen, sin ordenar ni recortar en el navegador', async () => {
    const origen = new OrigenEspia([{ id: 'b', nombre: 'B', total: 2 }, { id: 'a', nombre: 'A', total: 1 }]);
    const f = await montar(origen);
    f.componentInstance.alternarOrden('nombre');
    await f.whenStable();
    f.detectChanges();
    const nombres = [...f.nativeElement.querySelectorAll('tbody tr td:nth-child(2)')].map((td: HTMLElement) => td.textContent?.trim());
    expect(nombres).toEqual(['B', 'A']); // el orden lo decide el origen, no la tabla
    expect(origen.consultas.at(-1)!.orden).toEqual([{ campo: 'nombre', desc: false }]);
    expect(origen.consultas.at(-1)!.tamano).toBe(20);
  });

  it('pide la página siguiente al origen y muestra el total del filtro', async () => {
    const origen = new OrigenEspia([{ id: 'a', nombre: 'A', total: 1 }]);
    const f = await montar(origen);
    f.componentInstance.cambiarPagina(1);
    await f.whenStable();
    f.detectChanges();
    expect(origen.consultas.at(-1)!.pagina).toBe(1);
    expect(f.nativeElement.querySelector('.o_pager').textContent).toContain('21-40');
    expect(f.nativeElement.querySelector('tfoot').textContent).toContain('999.0');
  });

  it('vuelve a la primera página al cambiar la búsqueda', async () => {
    const origen = new OrigenEspia([]);
    const f = await montar(origen);
    f.componentInstance.cambiarPagina(2);
    await f.whenStable();
    f.componentInstance.busqueda.set('x');
    f.detectChanges();
    await f.whenStable();
    expect(origen.consultas.at(-1)).toMatchObject({ pagina: 0, busqueda: 'x' });
  });

  it('mueve columnas entre las visibles, saltando las ocultas', async () => {
    const f = await montar(new OrigenEspia([]));
    const l = f.componentInstance;
    l.ordenColumnas.set(['a', 'b', 'c']);
    l.visibilidad.set({ b: false });
    l.moverColumna('c', -1);
    expect(l.ordenColumnas()).toEqual(['c', 'b', 'a']);
  });
});
