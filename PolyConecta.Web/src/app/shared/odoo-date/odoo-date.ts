import { Component, computed, forwardRef, input, signal, viewChild } from '@angular/core';
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
      <button type="button" brnPopoverTrigger class="form-control form-control-sm text-start d-flex align-items-center gap-2"
              [disabled]="soloLectura() || deshabilitado()" [attr.aria-label]="nombre() + ': ' + texto()">
        <pc-odoo-icon nombre="calendario" />
        <span data-fecha-texto [class.text-muted]="!valor()">{{ texto() }}</span>
      </button>
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
