import { Component } from '@angular/core';
import { MainLayout } from './shared/main-layout/main-layout';

/** Raíz: el layout envuelve todas las rutas, como DefaultLayout en Routes.razor. */
@Component({
  selector: 'app-root',
  imports: [MainLayout],
  template: '<pc-main-layout />',
  styles: ':host { display: contents; }',
})
export class App {}
