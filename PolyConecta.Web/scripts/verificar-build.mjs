// npm run verificar-build: corre después de `npm run build` (spec 011, L2-T002).
// Falla si el CSS trae reglas de Tailwind (Spartan lo instala como dependencia, pero no debe entrar
// al build) o si la carga inicial comprimida pasa del límite del plan (74 kB medidos así antes de la spec 011, más 15 kB).
import { readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { gzipSync } from 'node:zlib';

const DIR = 'dist/polyconecta-web/browser';
const LIMITE_KB = 89;
const errores = [];

for (const css of readdirSync(DIR).filter(f => f.endsWith('.css'))) {
  if (readFileSync(join(DIR, css), 'utf8').includes('--tw-')) errores.push(`${css} contiene reglas de Tailwind`);
}

// Carga inicial: los scripts y hojas que index.html pide directamente.
const index = readFileSync(join(DIR, 'index.html'), 'utf8');
const iniciales = [...new Set([...index.matchAll(/(?:src|href)="([^"]+\.(?:js|css))"/g)].map(m => m[1]))].filter(f => !f.startsWith('http'));
const kb = iniciales.reduce((t, f) => t + gzipSync(readFileSync(join(DIR, f))).length, 0) / 1024;
console.log(`Carga inicial: ${kb.toFixed(1)} kB comprimidos (${iniciales.join(', ')}); límite ${LIMITE_KB} kB`);
if (kb > LIMITE_KB) errores.push(`la carga inicial pesa ${kb.toFixed(1)} kB comprimidos, más que ${LIMITE_KB}`);

if (errores.length) {
  console.error(errores.map(e => `✘ ${e}`).join('\n'));
  process.exit(1);
}
console.log('✓ Build verificado');
