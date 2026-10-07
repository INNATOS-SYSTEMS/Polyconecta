import { Component, computed, signal, viewChild } from '@angular/core';
// BrnComboboxImports trae BrnCombobox y BrnComboboxMultiple con el mismo selector: se importan una por una.
import { BrnCombobox, BrnComboboxAnchor, BrnComboboxContent, BrnComboboxEmpty, BrnComboboxInput, BrnComboboxItem, BrnComboboxList, BrnComboboxPopoverTrigger } from '@spartan-ng/brain/combobox';
import { BrnPopover, BrnPopoverImports } from '@spartan-ng/brain/popover';
import { BrnCalendar, BrnCalendarImports, provideBrnCalendarI18n } from '@spartan-ng/brain/calendar';
import { provideNativeDateAdapter } from '@spartan-ng/brain/date-time';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

interface Producto { clave: string; nombre: string }
const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
const DIAS = ['do', 'lu', 'ma', 'mi', 'ju', 'vi', 'sá'];

@Component({
  selector: 'app-campos-prueba',
  imports: [BrnCombobox, BrnComboboxAnchor, BrnComboboxContent, BrnComboboxEmpty, BrnComboboxInput, BrnComboboxItem, BrnComboboxList, BrnComboboxPopoverTrigger, BrnPopoverImports, BrnCalendarImports, LucideChevronLeft, LucideChevronRight],
  providers: [
    provideNativeDateAdapter(),
    provideBrnCalendarI18n({
      formatWeekdayName: i => DIAS[i],
      formatHeader: (m, y) => `${MESES[m]} ${y}`,
      formatMonth: m => MESES[m],
      firstDayOfWeek: () => 1,
      labelPrevious: () => 'Mes anterior',
      labelNext: () => 'Mes siguiente',
      labelWeekday: i => ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'][i],
    }),
  ],
  // brain no aplica estilos: ocultar opciones filtradas y el aviso "Sin resultados" es nuestro.
  styles: ['[data-hidden] { display: none !important; }', '[brnComboboxContent]:not([data-empty]) [brnComboboxEmpty], [brnComboboxContent][data-empty="false"] [brnComboboxEmpty] { display: none; }'],
  template: `
    <h6>Selección de registro (combobox)</h6>
    <div brnCombobox brnPopover [(value)]="producto" [itemToString]="aTexto" data-prueba="combo" style="width:420px">
      <div brnComboboxAnchor>
        <input brnComboboxInput brnComboboxPopoverTrigger [closeOnTriggerClick]="false" class="form-control" placeholder="Clave o producto" data-prueba="combo-input">
      </div>
      <div *brnPopoverContent class="bg-white border rounded shadow-sm" style="width:420px">
        <div brnComboboxContent>
          <div brnComboboxList class="list-group" data-prueba="combo-lista">
            @for (p of productos; track p.clave) {
              <div brnComboboxItem [value]="p" class="list-group-item" [attr.data-prueba]="'op-' + p.clave">{{ p.clave }} · {{ p.nombre }}</div>
            }
          </div>
          <div brnComboboxEmpty class="p-2 text-muted" data-prueba="combo-vacio">Sin resultados</div>
        </div>
      </div>
    </div>
    <div data-prueba="combo-valor">{{ producto()?.clave ?? '—' }}</div>

    <h6 class="mt-4">Fecha (calendario)</h6>
    <div brnPopover>
      <button type="button" brnPopoverTrigger class="btn btn-outline-secondary btn-sm" data-prueba="fecha-boton">{{ fechaTexto() }}</button>
      <div *brnPopoverContent class="bg-white border rounded shadow-sm p-2">
        <div brnCalendar [date]="fecha()" (dateChange)="elegir($event)" data-prueba="calendario">
          <div class="d-flex justify-content-between align-items-center">
            <button type="button" brnCalendarPreviousButton class="btn btn-sm"><svg lucideChevronLeft></svg></button>
            <div brnCalendarHeader data-prueba="cal-titulo">{{ titulo() }}</div>
            <button type="button" brnCalendarNextButton class="btn btn-sm"><svg lucideChevronRight></svg></button>
          </div>
          <table brnCalendarGrid>
            <thead><tr><th *brnCalendarWeekday="let d">{{ d }}</th></tr></thead>
            <tbody>
              <tr *brnCalendarWeek="let week">
                @for (d of week; track d.getTime()) {
                  <td brnCalendarCell><button type="button" brnCalendarCellButton [date]="d" class="btn btn-sm" [attr.data-prueba]="'dia-' + d.getDate() + '-' + d.getMonth()">{{ d.getDate() }}</button></td>
                }
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>`,
})
export class CamposPrueba {
  readonly productos: Producto[] = [
    { clave: 'PT1113 C567', nombre: 'BOLSA MEDIANA 44X84' }, { clave: 'PT3413 C455', nombre: 'ROLLO IMPRESO' },
    { clave: 'MP0001', nombre: 'POLIETILENO BAJA DENSIDAD' }, { clave: 'MP0002', nombre: 'PIGMENTO BLANCO' },
  ];
  private readonly calendario = viewChild(BrnCalendar);
  readonly titulo = computed(() => { const d = this.calendario()?.focusedDate() as Date | undefined; return d ? `${MESES[d.getMonth()]} ${d.getFullYear()}` : ''; });
  readonly producto = signal<Producto | null>(null);
  readonly aTexto = (p: Producto) => `${p.clave} · ${p.nombre}`;
  readonly fecha = signal<Date>(new Date(2026, 9, 7));
  readonly fechaTexto = computed(() => new Intl.DateTimeFormat('es-MX', { dateStyle: 'medium' }).format(this.fecha()));
  elegir(d: Date): void { this.fecha.set(d); }
}
// BrnPopover se importa también como tipo para que el combobox lo encuentre como directiva del mismo elemento.
export type _Popover = BrnPopover;
