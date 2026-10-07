import { existsSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

interface Resultado {
  ruta: string;
  nombre: string;
  proporcion: number;
  pasa: boolean;
}

/** Arma parity-report/index.html con cada ruta: capturas, mapa de diferencias y porcentaje (FR-017). */
export default function informe(): void {
  const dir = join(__dirname, '..', '..', 'parity-report');
  if (!existsSync(dir)) return;
  const resultados: Resultado[] = readdirSync(dir)
    .filter(f => f.endsWith('.json'))
    .map(f => JSON.parse(readFileSync(join(dir, f), 'utf8')) as Resultado)
    .sort((a, b) => a.ruta.localeCompare(b.ruta));
  const filas = resultados
    .map(
      r => `<tr class="${r.pasa ? 'ok' : 'mal'}"><td>${r.ruta}</td><td>${(r.proporcion * 100).toFixed(2)} %</td>
      <td><a href="${r.nombre}.blazor.png">Blazor</a> · <a href="${r.nombre}.angular.png">Angular</a> · <a href="${r.nombre}.diff.png">diferencias</a></td></tr>`,
    )
    .join('\n');
  writeFileSync(
    join(dir, 'index.html'),
    `<!doctype html><meta charset="utf-8"><title>Paridad Blazor · Angular</title>
<style>body{font-family:system-ui;margin:2rem}td{padding:.3rem .8rem}.ok td:nth-child(2){color:green}.mal td:nth-child(2){color:#b00;font-weight:bold}</style>
<h1>Paridad Blazor · Angular</h1><p>${resultados.filter(r => r.pasa).length} de ${resultados.length} rutas con ≤ 1 % de píxeles distintos. Excepciones documentadas: <a href="excepciones.md">excepciones.md</a>.</p>
<table><tr><th>Ruta</th><th>Diferencia</th><th>Capturas</th></tr>${filas}</table>`,
  );
}
