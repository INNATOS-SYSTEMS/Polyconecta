import { guion } from './runner';

/** Comprueba el arnés: la barra superior se lee igual en las dos aplicaciones. */
guion({
  nombre: 'barra superior de fabricación',
  pasos: [
    { ir: '/fabricacion' },
    { control: 'módulo', en: '.o_module_name' },
    { control: 'menú', en: '.o_header_menu' },
  ],
});
