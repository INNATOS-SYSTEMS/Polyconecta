# Investigación: Presentación en Angular

Decisiones de la fase 0 del plan. Cada una se verificó contra el código de `PolyConecta.Presentation` o contra el registro de npm el 2026-10-05.

---

## R-01 · Versiones exactas

**Decisión**:

| Paquete | Versión | Motivo |
| :--- | :--- | :--- |
| `@angular/*` | 22.2.1 | Última de Angular 22 (D-68) |
| `typescript` | 6.0.3 | Angular 22.2.1 exige `>=6.0 <6.1`. La 7.0 ya existe, pero Angular no la admite |
| Node | 24.16.0 | Angular 22 pide `^24.15.0`; es la versión instalada (D-69) |
| `bootstrap` / `bootstrap-icons` | 5.3.2 / 1.11.3 | Las mismas que carga `App.razor` (FR-002) |
| `@microsoft/signalr` | 10.0.11 | La misma versión mayor que ASP.NET Core 10 |
| `@playwright/test` | 1.63.0 | Última estable |
| `pixelmatch` / `pngjs` | 7.2.0 / 7.0.0 | Últimas estables |

**Por qué**: CT-36 pide versiones exactas, y la regla de dependencias de la spec solo aprueba estos paquetes.

**Alternativas**: TypeScript 7.0, descartada porque Angular 22 no la admite.

---

## R-02 · Documentos únicos del prototipo y el modo libre

**Hallazgo**: el prototipo no tiene colecciones de pedidos, traslados, recepciones ni entregas. `OperationalFlowState` guarda **un** pedido (`IV310-26`, con `CurrentOrderStage` y `PedidoLineas`), **un** `Traslado`, **una** `Recepcion` y **una** `Entrega`. Las OF, las operaciones de almacén (`StockOperationState.Operaciones`) y las incidencias sí son listas.

**Decisión**: en Angular, los cuatro documentos únicos pasan a ser colecciones con un **documento semilla idéntico** al del prototipo. Las operaciones del prototipo (`autorizar`, `validarTraslado`…) reciben el folio del documento sobre el que actúan, y sobre la semilla se comportan igual que en Blazor. "Nuevo" agrega un documento a la colección.

**Por qué**: sin colección, "Nuevo" (FR-012, Principio X) no tiene dónde crear un segundo documento. La paridad de US-1 y US-2 se mide sobre los datos semilla, que no cambian.

**Alternativas**:
- Reemplazar el documento único al crear uno nuevo: destruye la semilla y rompe los guiones de US-2.
- Dejar "Nuevo" fuera para esos cuatro documentos: incumple D-59.

**Efecto en la spec**: FR-006 ("portar 1:1") se lee como "1:1 en comportamiento sobre la semilla". Está registrado en "Exploración y cambios".

---

## R-03 · Estado al recargar (FR-011)

**Hallazgo**: `PolyConecta.Presentation/Program.cs` registra los cuatro servicios de estado como `AddScoped`. En Blazor Server, el ámbito es el circuito, y recargar la pestaña abre un circuito nuevo. El prototipo **pierde el estado al recargar**. No hay `sessionStorage`, `localStorage` ni `PersistentComponentState` en el código.

**Decisión**: el estado de Angular vive solo en memoria y se pierde al recargar. No se usa `sessionStorage`.

**Por qué**: FR-011 pide comportarse igual que Blazor.

**Efecto en la spec**: el escenario 6 de US-2 queda como "al recargar, el estado vuelve a la semilla, igual que en Blazor". Está registrado en "Exploración y cambios".

---

## R-04 · Qué componentes puede hacer la tarea 0.5

**Hallazgo**: de los 13 componentes, 12 reciben datos por parámetros. Sin embargo, 5 de ellos usan tipos del estado como parámetro:

- `LotQuantityPickerModal`: `StockOperationLine`, `LotBalance` y `LotAllocation`
- `LotPickerModal`: `ProductionLot`
- `OdooLineCapture`: `ProductRef`
- `OdooViewSwitcher`: `UiViewState`

`PocSalesOrderForm` **inyecta** `OperationalFlowState` e `InventoryState`. Es la mitad del formulario del pedido, no un componente genérico.

**Decisión**:
- La tarea 0.5 hace 12 componentes y crea en `src/app/core/models/` **solo los tipos** que esos componentes usan, más `UiViewState`, que no depende de nada.
- `PocSalesOrderForm` pasa a la fase 3A (pedidos) de esta spec.
- La fase 1 agrega los servicios de estado y la semilla sin cambiar esos tipos. Si necesita cambiarlos, lo registra en `bloqueos.md` (regla de autonomía 3).

**Por qué**: la 0.5 corre antes que la fase 1 y no puede construir un componente que inyecta servicios que todavía no existen.

**Efecto en la spec 002**: FR-022 dice "los 13 componentes". Hay que corregirlo a 12 en la exploración de la 002, en su propia rama (CT-44). Queda como pendiente para el líder del camino 2.

---

## R-05 · Fases 0 y 2A, y de dónde sale la rama

**Hallazgo**: FR-022 de la spec 002 asigna a la tarea 0.5 las fases 0 y 2A de esta spec. La regla de ramas (CT-44) solo deja integrar a `main` lo terminado, y la 002 cierra después del 9 de octubre.

**Decisión**: `001-angular-presentation` sale de `main`. Hasta que la 002 se integre, esta rama solo lleva documentación (spec, plan y tareas). Cuando la 002 se integre, la 001 trae `main` a su rama y empieza la fase 1.

**Alternativas**: sacar la rama de `002-construccion-tecnica` al terminar la 0.5. Empieza unos días antes, pero ata la 001 a una rama sin cerrar, que puede seguir cambiando. Queda como opción si el líder la pide (pregunta abierta 2 de la spec).

---

## R-06 · Runner de pruebas unitarias

**Hallazgo**: Angular 22 corre `ng test` con **Vitest** a través de `@angular/build:unit-test`. `@angular/build` 22.2.1 declara `vitest ^4.0.8 || ^5.0.0` como dependencia par. Karma está obsoleto. Vitest necesita un DOM simulado (`jsdom`) para probar componentes.

**Decisión**: usar Vitest 5.0.3 y jsdom 30.1.2. El usuario los aprobó el 2026-10-05 y quedaron en la lista de la regla de autonomía 4.

**Alternativas**:
- Karma con Jasmine: obsoleto en Angular 22.
- Probar las reglas solo con Playwright: lento, y no cubre SC-003 de forma aislada.

---

## R-07 · Chatter en vivo (US-4, FR-015, FR-016)

**Hallazgo**: el chatter del prototipo **no usa el hub**. `OdooChatterDrawer.Send()` solo agrega el mensaje a la lista local, con autor `Administrator` y hora `h:mm tt`. `ChatterHub` existe y está mapeado en `/hubs/chatter`, pero ninguna página se conecta a él. US-4 dice "en el prototipo ya funciona", y no es así: el chatter en vivo es **comportamiento nuevo**, como "Nuevo".

**Decisión**:
- **API**: copia del hub sin cambios de contrato ([contracts/chatter-hub.md](contracts/chatter-hub.md)). La política de CORS se llama así y admite el origen `http://localhost:4200` **con credenciales**. La política actual (`AllowAnyOrigin`) no sirve: el cliente de SignalR negocia con credenciales y el navegador rechaza `*` con credenciales.
- **Cliente**: con conexión, el mensaje se envía por el hub y se muestra al recibirse, filtrado por `documentId`, sin duplicar el propio. Sin conexión, se agrega local, como en Blazor, y el panel indica que no hay conexión en vivo.
- **Hora**: el cliente muestra la hora local de recepción con el formato de Blazor (`h:mm tt`) e ignora la cadena del servidor (`DateTime.UtcNow.ToString("g")`), que depende de la cultura y viene en UTC.
- **Paridad**: las capturas y los guiones corren con la API apagada, así que el chatter se ve igual que en Blazor. US-4 se prueba aparte con dos pestañas.

**Efecto en la spec**: el "Why" de US-4 y la tabla de alcance se corrigen en "Exploración y cambios".

---

## R-08 · Arnés de paridad (FR-017, FR-018)

**Decisión**:
- **Capturas**: Playwright con Chromium a 1600×900, `reducedMotion: 'reduce'` y CSS que apaga transiciones.
- **Antes de capturar**: espera `document.fonts.ready`. Si Inter o los CSS del CDN no cargan, la prueba falla con ese motivo (caso borde de la spec).
- **En Blazor**: espera además a que el circuito esté conectado. Basta con que aparezca un elemento interactivo de la página.
- **Botón "Nuevo"**: se excluye con la opción `mask` de Playwright en las dos capturas.
- **Comparación**: `pixelmatch` con su `threshold` por píxel predeterminado (0.1). La ruta pasa si los píxeles distintos son ≤ 1 % del total.
- **Fechas**: `DateTime.Today` (incidencias) y "Hoy" (chatter) salen del reloj de la máquina en las dos aplicaciones, así que corren en la misma máquina y no se congela el reloj.
- **Guiones**: cada guion es una lista de pasos (`ir a`, `pulsar`, `capturar`) con puntos de control que leen el texto visible de un contenedor. Se ejecuta igual contra `:9000` y `:4200`, y se comparan los textos.

---

## R-09 · Pedido libre y D-124

**Hallazgo**: D-124 (posterior a la spec) dice que toda línea guarda la cantidad en la unidad de CONTPAQi **y** el peso en kg, los dos capturados. FR-012 pide para el pedido libre cantidad, unidad, precio unitario y moneda, sin peso.

**Decisión** (del usuario, 2026-10-05): **el pedido libre de la réplica captura los dos valores**: la cantidad en la unidad de CONTPAQi del producto y el peso en kg, a mano y sin que uno se calcule del otro, aunque la réplica no esté conectada a la API. FR-012 se actualiza. Las líneas de la semilla no cambian, para no romper la paridad.

**Alternativa descartada**: dejar el kg para la spec 003, al conectar la API.

---

## R-10 · `run.sh`

**Hallazgo**: el `./run.sh` de la raíz solo delega en `scripts/run.sh`. Hoy `scripts/run.sh` busca el runtime de ASP.NET Core 8, y la tarea 0.3 lo cambia a .NET 10.

**Decisión**: FR-003 queda igual. `--with-angular` se agrega a `scripts/run.sh` en la fase 5, sobre la versión que deje la 0.3, y funciona igual desde `./run.sh`.
