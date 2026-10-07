# Quickstart: verificar la spec 011

Cómo comprobar que cada parte está terminada. Se corre en `PolyConecta.Web` con Node 24.16.

## 1. Base y galería (P1)

```bash
cd PolyConecta.Web && npm ci
npm run build && npm test
npm start                      # http://localhost:9000/catalogo
```

**Esperado**:
- `package.json` tiene las cinco librerías de research R-01 con versión exacta, y `npm audit` no reporta vulnerabilidades.
- `npm run verificar-build` pasa: el CSS no tiene reglas de Tailwind y la carga inicial está dentro de lo permitido.
- `/catalogo` muestra cada componente del [contrato](contracts/componentes.md) en todos sus estados.
- La lista de la galería agrupa, ordena, filtra, pagina, oculta y reordena columnas, selecciona, exporta y guarda favoritos.
- El kanban de la galería mueve una tarjeta con una transición válida, regresa una inválida y abre el diálogo de una que pide datos.
- El combobox filtra, se maneja con teclado y muestra "Buscar más…" y "Sin resultados"; el calendario está en español.
- El estado de sincronización muestra sus cinco estados.

## 2. Pantallas migradas (cada parte)

```bash
npm run scenarios              # guiones contra el prototipo: textos y flujos
npm run audit                  # botones, enlaces y navegación contra el prototipo
npx playwright test --config e2e/catalogo/playwright.config.ts
npm run tablero                # recaptura las pantallas para el lienzo
```

**Esperado**:
- Los escenarios y la auditoría siguen en verde: la estructura y los flujos no cambiaron.
- En las pantallas de la parte, la lista, el kanban y los campos son los componentes nuevos.
- No queda ningún ícono de Bootstrap (`grep -rn 'bi-' src/app/features/<flujo>` vacío).
- El "Nuevo" de sus documentos tiene la estructura completa (D-136): etapas en Borrador, maestro, pestañas, detalle con captura y chatter que se activa al guardar.

## 3. Cierre (P8)

- `grep -rn 'bi-' src/app` está vacío y `index.html` ya no carga Bootstrap Icons.
- No existe `npm run parity` ni `e2e/parity/`.
- `docs/diseno/07-contratos-visuales.md` tiene todos los contratos de la spec, y CT-24 apunta a él.
- El lienzo está regenerado con las 28 pantallas migradas y la galería.
