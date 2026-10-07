import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { SearchView } from '../../core/search/search-view';
import { OdooSearchPanel } from './odoo-search-panel';

interface Fila {
  folio: string;
  estado: string;
}

const VISTA: SearchView<Fila> = {
  campos: [{ etiqueta: 'Folio', valor: f => f.folio }],
  filtros: [
    { nombre: 'Borrador', campo: 'Estado', condicion: f => f.estado === 'Borrador' },
    { nombre: 'Hecho', campo: 'Estado', condicion: f => f.estado === 'Hecho' },
  ],
  agrupaciones: [],
};

describe('OdooSearchPanel', () => {
  it('propone los campos de la vista y agrupa los filtros activos del mismo campo con "o"', () => {
    const fixture = TestBed.createComponent(OdooSearchPanel<Fila>);
    fixture.componentRef.setInput('view', VISTA);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect((el.querySelector('input') as HTMLInputElement).placeholder).toBe('Buscar por Folio...');

    fixture.componentRef.setInput('filtrosActivos', ['Borrador', 'Hecho']);
    fixture.detectChanges();
    expect(el.querySelector('.o_search_facet_value')?.textContent).toBe('Borrador o Hecho');
    expect((el.querySelector('input') as HTMLInputElement).placeholder).toBe('');

    (el.querySelector('.o_search_facet_remove') as HTMLButtonElement).click();
    expect(fixture.componentInstance.filtrosActivos()).toEqual([]);
  });
});
