import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';
import { OdooSystray } from './odoo-systray';
import { SesionState } from '../../core/sesion/sesion-state';
import { MenuItem, moduleFor } from './modulos';

/**
 * Réplica de Components/Shell/OdooTopbar.razor, estructurada (contratos visuales §2.2): a la izquierda el
 * hub, el nombre del módulo (o "PolyConecta" si no hay módulo) y su menú; a la derecha, mensajes y el
 * usuario (`pc-odoo-systray`, diferido). Va en la carga inicial: sus menús son propios, sin CDK, y sus
 * íconos van en línea.
 */
@Component({
  selector: 'pc-odoo-topbar',
  imports: [RouterLink, OdooSystray],
  templateUrl: './odoo-topbar.html',
  styles: ':host { display: contents; }',
})
export class OdooTopbar {
  private readonly router = inject(Router);
  private readonly sesion = inject(SesionState);

  private readonly path = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects.split(/[?#]/)[0]),
    ),
    { initialValue: this.router.url.split(/[?#]/)[0] },
  );

  protected readonly currentModule = computed(() => moduleFor(this.path()));
  protected readonly visibleItems = computed(() => {
    return this.currentModule().items
      .filter(item => !item.permiso || this.sesion.tienePermiso(item.permiso))
      .map(item => {
        if (!item.children) return item;
        const children = item.children.filter(c => !c.permiso || this.sesion.tienePermiso(c.permiso));
        return { ...item, children };
      })
      .filter(item => !item.children || item.children.length > 0);
  });

  protected readonly menuAbierto = signal<string | null>(null);
  protected readonly showMenu = computed(() => {
    const items = this.visibleItems();
    return items.length > 1 || items.some(i => i.children !== undefined);
  });

  constructor() {
    if (!this.sesion.conSesion()) {
      this.sesion.cargarSesion().subscribe();
    }
    // Al navegar se cierra cualquier menú abierto, como en OnLocationChanged.
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => this.menuAbierto.set(null));
  }

  protected isActive(route: string): boolean {
    return this.path().toLowerCase().startsWith(route.toLowerCase());
  }

  protected anyChildActive(item: MenuItem): boolean {
    return (item.children ?? []).some(c => this.isActive(c.route));
  }

  protected toggle(label: string): void {
    this.menuAbierto.update(actual => (actual === label ? null : label));
  }

  protected goHome(): void {
    void this.router.navigateByUrl('/');
  }
}
