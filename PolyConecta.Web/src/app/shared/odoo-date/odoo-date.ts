import { Component, computed, forwardRef, input, signal, viewChild, ViewEncapsulation } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { BrnCalendar, BrnCalendarImports, provideBrnCalendarI18n } from '@spartan-ng/brain/calendar';
import { provideNativeDateAdapter } from '@spartan-ng/brain/date-time';
import { BrnPopover, BrnPopoverContent, BrnPopoverTrigger } from '@spartan-ng/brain/popover';
import { OdooIcon } from '../odoo-icon/odoo-icon';

const MESES = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];
const DIAS = ['do', 'lu', 'ma', 'mi', 'ju', 'vi', 'sá'];
const DIAS_LARGOS = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'];
const FORMATO = new Intl.DateTimeFormat('es-MX', { dateStyle: 'medium' });

/**
 * Fecha (spec 011, research R-04): calendario de Spartan en un popover, en español, con la semana desde
 * el lunes. brain no trae textos ni estilos: los textos de accesibilidad se dan aquí (por omisión vienen
 * en inglés) y el aspecto está en `app.css` (`.o_calendar`).
 */
@Component({
  selector: 'pc-odoo-date',
  imports: [BrnCalendarImports, BrnPopover, BrnPopoverContent, BrnPopoverTrigger, OdooIcon],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => OdooDate), multi: true },
    provideNativeDateAdapter(),
    provideBrnCalendarI18n({
      formatWeekdayName: i => DIAS[i],
      formatHeader: (m, y) => `${MESES[m]} ${y}`,
      formatMonth: m => MESES[m],
      firstDayOfWeek: () => 1,
      labelPrevious: () => 'Mes anterior',
      labelNext: () => 'Mes siguiente',
      labelWeekday: i => DIAS_LARGOS[i],
    }),
  ],
  template: `
    <div brnPopover align="start" [attr.data-fecha]="nombre()">
      <div class="o_field" [class.o_field_solo_lectura]="soloLectura()">
        <button type="button" brnPopoverTrigger class="o_field_valor d-flex align-items-center gap-2"
                [disabled]="soloLectura() || deshabilitado()" [attr.aria-label]="nombre() + ': ' + texto()">
          <span data-fecha-texto class="flex-grow-1" [class.text-muted]="!valor()">{{ texto() }}</span>
          @if (!soloLectura()) { <pc-odoo-icon nombre="calendario" class="text-muted" /> }
        </button>
      </div>
      <div *brnPopoverContent="let ctx" class="o_dropdown_panel o_calendar">
        <div brnCalendar [date]="valor() ?? undefined" [min]="min() ?? undefined" [max]="max() ?? undefined" (dateChange)="elegir($event); ctx.close()">
          <div class="o_calendar_header">
            <button type="button" brnCalendarPreviousButton class="btn o_btn_icon"><pc-odoo-icon nombre="anterior" /></button>
            <div brnCalendarHeader data-fecha-mes>{{ titulo() }}</div>
            <button type="button" brnCalendarNextButton class="btn o_btn_icon"><pc-odoo-icon nombre="siguiente" /></button>
          </div>
          <table brnCalendarGrid>
            <thead><tr><th *brnCalendarWeekday="let d" scope="col" [attr.aria-label]="diasLargos[d]">{{ dias[d] }}</th></tr></thead>
            <tbody>
              <tr *brnCalendarWeek="let semana">
                @for (d of semana; track d.getTime()) {
                  <td brnCalendarCell class="text-center"><button type="button" brnCalendarCellButton [date]="d" class="o_calendar_dia" [attr.data-dia]="clave(d)">{{ d.getDate() }}</button></td>
                }
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
  // Estilos del calendario aquí y no en app.css: el campo carga con su pantalla, no en la carga inicial.
  // Sin encapsulación: los días los pinta la directiva del calendario.
  encapsulation: ViewEncapsulation.None,
  styles: `
    .o_calendar { padding: 0.5rem; width: 260px; }
    .o_calendar_header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.35rem; font-weight: 600; text-transform: capitalize; }
    .o_calendar table { width: 100%; border-collapse: collapse; }
    .o_calendar th { font-size: 0.72rem; color: var(--text-muted); font-weight: 600; text-align: center; padding: 0.2rem 0; }
    .o_calendar_dia {
        width: 32px; height: 32px; border: 0; border-radius: 6px; background: transparent; font-size: 0.82rem; color: var(--text-main);
    }
    .o_calendar_dia:hover { background: var(--brand-light); }
    .o_calendar_dia[data-today="true"] { font-weight: 700; color: var(--brand-primary); }
    .o_calendar_dia[data-selected="true"] { background: var(--brand-primary); color: white; }
    .o_calendar_dia[data-outside="true"] { opacity: 0.4; }
    .o_calendar_dia[data-disabled="true"] { opacity: 0.3; pointer-events: none; }
  `,
})
export class OdooDate implements ControlValueAccessor {
  readonly nombre = input('Fecha');
  readonly min = input<Date | null>(null);
  readonly max = input<Date | null>(null);
  readonly soloLectura = input(false);
  readonly obligatorio = input(false);

  protected readonly dias = DIAS;
  protected readonly diasLargos = DIAS_LARGOS;
  readonly valor = signal<Date | null>(null);
  readonly deshabilitado = signal(false);
  private readonly calendario = viewChild(BrnCalendar);
  protected readonly texto = computed(() => (this.valor() ? FORMATO.format(this.valor()!) : 'Elegir fecha'));
  protected readonly titulo = computed(() => {
    const d = this.calendario()?.focusedDate() as Date | undefined;
    return d ? `${MESES[d.getMonth()]} ${d.getFullYear()}` : '';
  });
  private alCambiar: (v: Date | null) => void = () => {};
  private alTocar: () => void = () => {};

  /** `aaaa-mm-dd` del día, para pruebas y accesibilidad. */
  protected clave(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  protected elegir(d: Date): void {
    this.valor.set(d);
    this.alCambiar(d);
    this.alTocar();
  }

  writeValue(v: Date | null): void { this.valor.set(v ?? null); }
  registerOnChange(fn: (v: Date | null) => void): void { this.alCambiar = fn; }
  registerOnTouched(fn: () => void): void { this.alTocar = fn; }
  setDisabledState(d: boolean): void { this.deshabilitado.set(d); }
}
