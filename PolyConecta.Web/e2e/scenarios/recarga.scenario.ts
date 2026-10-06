import { guion } from './runner';

/** US-2, escenario 6 (research R-03): al recargar, el estado vuelve a la semilla en las dos aplicaciones. */
guion({
  nombre: 'recarga vuelve a la semilla',
  pasos: [
    { ir: '/pedidos/IV310-26' },
    { control: 'semilla', en: 'main' },
    { pulsar: '.o_statusbar button', texto: 'Confirmar' },
    { control: 'modificado', en: 'main', esperado: /Autorizar/ },
    { recargar: true },
    { control: 'tras recargar', en: 'main', esperado: /Confirmar/ },
  ],
});
