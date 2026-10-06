# Quickstart: verificar la presentación en Angular

Cómo comprobar que la feature cumple sus criterios de éxito. Los scripts están descritos en [contracts/parity.md](contracts/parity.md).

## Requisitos

- Node 24.16.0 (`nvm use` dentro de `PolyConecta.Web/`).
- El SDK de .NET que fije `global.json` después de la tarea 0.3 de la spec 002.
- Chromium de Playwright: `npx playwright install chromium`.
- Conexión a Google Fonts y a jsDelivr: las dos aplicaciones cargan de ahí Inter, Bootstrap y Bootstrap Icons.

## 1. Levantar todo

```bash
./run.sh
```

Levanta Angular en `:9000` y la API en `:9200` (D-128). El prototipo Blazor ya no lo levanta `run.sh`: las pruebas de paridad lo arrancan en `:9010`.

## 2. Compilar y probar (FR-019, SC-003)

```bash
cd PolyConecta.Web
npm ci
npm run build
npm test
```

**Esperado**: los dos terminan con código 0. `npm test` muestra al menos una prueba por cada regla de [data-model.md §3](data-model.md#3-reglas-que-se-prueban-fr-008-fr-012).

## 3. Paridad visual (US-1, SC-001)

```bash
npm run parity
```

**Esperado**: las 19 rutas quedan en ≤ 1 % de píxeles distintos. El informe está en `parity-report/index.html`.

**A mano**: abre `/fabricacion/BOL-2026-0001` en una pestaña nueva, que debe cargar sin pasar por la lista. Luego navega y usa atrás y adelante del navegador, que se comportan igual que en el prototipo (`:9010`).

## 4. Flujo operativo (US-2, SC-002)

```bash
npm run scenarios
```

**Esperado**: los textos de cada punto de control coinciden entre Blazor y Angular.

**A mano**, en Angular:
1. Abre el pedido `IV310-26`, confírmalo y pulsa "Autorizar" dos veces. Pasa a Autorizado con las firmas de Comercial y de Cobranza.
2. En una OF con un lote "En revisión", intenta cerrar la producción. No pasa a Hecho.
3. Recarga la pestaña. El estado vuelve a la semilla, igual que en Blazor (research R-03).

## 5. Modo libre (US-3, SC-004)

**A mano**: en cada uno de los 9 documentos de FR-012, pulsa "Nuevo", captura los campos mínimos y guarda.

**Esperado**:
- El documento aparece en su lista, sin origen y con sus smart buttons de origen vacíos.
- Una OF libre nombra sus lotes con el folio de la OF raíz: `R001-BOL-2026-0007`.
- Una recepción libre solo ofrece lotes en `TRANS/*`.
- Un traslado o una entrega libres solo ofrecen lotes liberados.
- Una recolección libre deja saldo sin asignar en WIP.
- Cada línea libre pide la cantidad y muestra la unidad base del producto en CONTPAQi, que no se puede editar.
- Un pedido libre recibe un Contpaq ID simulado al confirmarse.

Los guiones de US-3 en `npm run scenarios` verifican lo mismo de forma automática, solo contra Angular.

## 6. Chatter en vivo (US-4, SC-005)

```bash
npm run chatter
```

**A mano**:
1. Abre el mismo formulario en dos pestañas y escribe en una. El mensaje aparece en la otra en menos de 1 s.
2. Apaga la API y escribe. El panel dice "Sin conexión en vivo" y el mensaje se agrega solo en esa pestaña.

## 7. Nada se rompió (SC-006)

```bash
dotnet build Polyconecta.slnx
dotnet test Polyconecta.slnx
git diff main --stat -- PolyConecta.Presentation/
```

**Esperado**: build y pruebas en verde, y ningún cambio en `PolyConecta.Presentation/`.
