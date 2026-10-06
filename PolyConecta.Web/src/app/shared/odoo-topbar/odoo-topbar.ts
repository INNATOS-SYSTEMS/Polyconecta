import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';
import { MenuItem, moduleFor } from './modulos';

/** Réplica de Components/Shell/OdooTopbar.razor. */
@Component({
  selector: 'pc-odoo-topbar',
  imports: [RouterLink],
  templateUrl: './odoo-topbar.html',
  styles: ':host { display: contents; }',
})
export class OdooTopbar {
  private readonly router = inject(Router);

  private readonly path = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects.split(/[?#]/)[0]),
    ),
    { initialValue: this.router.url.split(/[?#]/)[0] },
  );

  protected readonly currentModule = computed(() => moduleFor(this.path()));
  protected readonly menuAbierto = signal<string | null>(null);
  protected readonly showMenu = computed(() => {
    const items = this.currentModule().items;
    return items.length > 1 || items.some(i => i.children !== undefined);
  });

  constructor() {
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
