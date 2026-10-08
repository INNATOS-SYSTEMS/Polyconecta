# Modelo de datos: Contratos visuales (spec 011)

La spec no agrega entidades de negocio. Lo que define son los **tipos de la interfaz**: cómo una lista pide sus datos, cómo se guardan los favoritos y cómo un kanban conoce sus etapas y transiciones. Viven en `PolyConecta.Web/src/app/core/lista/` y `core/kanban/`.

---

## 1. Origen de datos de lista (`core/lista/origen.ts`)

### `ConsultaLista`

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `pagina` | `number` | Desde 0. Con agrupación, es la página de grupos de primer nivel |
| `tamano` | `number` | Filas por página: 20, 40, 80 o 200. Por omisión, 80, como Odoo |
| `orden` | `{ campo: string; desc: boolean }[]` | En orden de prioridad. Vacío = orden por omisión de la lista |
| `filtros` | `FiltroLista[]` | Combinados con Y. Los de un mismo campo se combinan con O (05 §7.1) |
| `nombrados` | `string[]` | Filtros con nombre de la vista de búsqueda (`core/search`), como en Odoo: el origen los evalúa por nombre. Los del mismo campo de la vista se unen con O |
| `busqueda` | `string \| null` | Texto libre de la barra de búsqueda, sobre los campos que la lista declara buscables |
| `agruparPor` | `string[]` | Niveles de agrupación, en orden: un campo o la etiqueta de una agrupación de la vista. Vacío = sin agrupar |
| `grupo` | `{ campo: string; valor: string }[]` | Ruta del grupo que se abre. Vacío = primer nivel |
| `ids` | `string[] \| null` | Solo esas filas: para exportar las seleccionadas |

### `FiltroLista`

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `campo` | `string` | Columna |
| `operador` | `'contiene' \| 'igual' \| 'entre' \| 'en'` | `entre` para fechas y números; `en` para listas de estados |
| `valor` | `string \| number \| [unknown, unknown] \| unknown[]` | Según el operador |

### `ResultadoLista<T>`

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `filas` | `T[]` | Vacío cuando la respuesta es de grupos |
| `grupos` | `GrupoLista[] \| null` | Presente cuando hay un nivel de agrupación por abrir en esa ruta |
| `total` | `number` | Total de filas, o de grupos, para el paginador |
| `totales` | `Record<string, number>` | Sumas de las columnas sumables sobre todo el filtro, para el pie de la tabla |

### `GrupoLista`

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `campo`, `valor` | `string` | Qué agrupa. `valor` vacío se muestra como "Ninguno" |
| `etiqueta` | `string` | Texto a mostrar, por ejemplo el nombre del cliente |
| `cantidad` | `number` | Filas del grupo |
| `totales` | `Record<string, number>` | Sumas de las columnas sumables del grupo |

### `OrigenDeLista<T>`

```ts
interface OrigenDeLista<T> {
  consultar(consulta: ConsultaLista): Promise<ResultadoLista<T>>;
}
```

- **`OrigenEnMemoria<T>`** (esta spec): filtra, ordena, agrupa y pagina sobre la colección de un servicio de estado. Se crea con la colección, sus columnas sumables y la función que lee un campo.
- **`OrigenHttp<T>`** (F1): manda la misma consulta a la API. La ruta y el formato los fija F1 con su primera lista (spec, "Preguntas abiertas").

**Regla**: la tabla y el kanban **nunca** ordenan, filtran, agrupan ni paginan por su cuenta. Lo verifica una prueba que cuenta las consultas, como en la prueba técnica.

---

## 2. Favoritos (`core/lista/favoritos.ts`)

### `Favorito` (la forma de `SavedSearch`, 04 §3)

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `id` | `string` | Generado al guardar |
| `lista` | `string` | Llave de la lista, por ejemplo `ventas.pedidos` |
| `nombre` | `string` | Obligatorio y único por lista |
| `filtros`, `busqueda`, `agruparPor`, `orden` | los de `ConsultaLista` | |
| `columnas` | `{ campo: string; visible: boolean }[]` | En el orden elegido |
| `tamano` | `number` | |
| `porOmision` | `boolean` | Se aplica al abrir la lista. Solo uno por lista |

### `AlmacenDeFavoritos`

```ts
interface AlmacenDeFavoritos {
  listar(lista: string): Promise<Favorito[]>;
  guardar(f: Favorito): Promise<void>;
  borrar(lista: string, id: string): Promise<void>;
}
```

- **`FavoritosEnNavegador`** (esta spec): `localStorage`, con la llave `polyconecta.favoritos.<lista>`. Si guardar falla, responde con un error y la lista avisa.
- **`FavoritosEnServidor`** (F1): por usuario, en la base.

---

## 3. Kanban (`core/kanban/`)

### `EtapaKanban`

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `valor` | `string` | Estado del documento, o valor del campo de agrupación |
| `titulo` | `string` | Encabezado de la columna |
| `plegada` | `boolean` | Las etapas terminales (Hecho, Cancelado) empiezan plegadas |

### `TransicionKanban`

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `desde`, `hacia` | `string` | Estados |
| `nombre` | `string` | Nombre de la transición, el mismo del botón del formulario |
| `dialogo` | componente o `null` | Si la transición pide datos, el diálogo que los captura |
| `pideDialogo` | `(fila) => boolean`, opcional | Si se declara, el diálogo solo se abre cuando devuelve `true`. Recolección: validar pide el diálogo de cantidades solo si es parcial (P3) |
| `ejecutar` | `(folio, datos?) => string \| undefined` | Llama al **mismo** método del servicio de estado que el botón. Devuelve el motivo si no procede |

Un movimiento sin transición declarada regresa la tarjeta con el motivo "No se puede pasar de X a Y".

### Etapas y transiciones por lista

Salen de los estados que ya existen en `core/models` y `core/state`. La tabla de cada lista se confirma en su parte, contra los métodos reales.

| Lista | Etapas | Transiciones con arrastre | Parte |
| :--- | :--- | :--- | :---: |
| Pedidos | Borrador, Confirmado, Autorizado, En progreso, Hecho | Borrador → Confirmado (Confirmar); Confirmado → Autorizado (Autorizar, con diálogo de firma) | P1 |
| Fabricación | Borrador, Planeado, En progreso, Hecho | Borrador → Planeado (Confirmar: requiere componentes y libera la recolección); En progreso → Hecho (Cerrar producción: hard-stop de Calidad y saldo de WIP en cero). Planeado → En progreso la avanza el sistema al planear | P2 |
| Incidencias | Por centro de trabajo | Ninguna: no tienen estado | P2 |
| Recolecciones | Borrador, En espera, Listo, Hecho | Listo → Hecho (Validar, con diálogo de cantidades si es parcial, el mismo que abre el botón del formulario). Borrador → En espera lo avanza el sistema al confirmar la OF, y En espera → Listo, al declarar lotes | P3 |
| Calidad | Planeado, Parcial, Aprobado (por control, como la lista) | Ninguna: el estado sale de los lotes (aclaración P4). Se aprueba o falla lote por lote en el formulario; fallar pide confirmación | P4 |
| Traslados | Borrador, En espera de operación, En espera, Listo, Hecho | Listo → Hecho (Validar: cierra la salida de inventario; sin lotes, la tarjeta regresa con el motivo). En el formulario, "Validar" avanza una etapa por clic, como el prototipo; las etapas previas no se arrastran | P5 |
| Recepción | Borrador, En espera, Listo, Hecho | Listo → Hecho (Validar, con el diálogo "Validar recepción": los lotes que entran y la regla de D-56, solo lotes en tránsito; el mismo que abre el botón en Listo) | P6 |
| Entregas | Borrador, En espera, Listo, Hecho | Listo → Hecho (Validar). Hard-stop de Calidad: un lote elegido que no está liberado bloquea el cierre; el kanban regresa la tarjeta con el motivo y el formulario lo muestra con `OdooHardStop` | P7 |

Las transiciones que avanza el sistema, como Autorizado → En progreso, que ocurre cuando arranca la producción, no se pueden arrastrar.

---

## 4. Variables de botones (E3)

Ya están en `src/styles/app.css`; la tabla está en la spec, E3. Los componentes nuevos usan esas variables y no escriben colores sueltos.

---

## 5. Lo que no cambia

- Los servicios de estado (`core/state/`) y sus reglas siguen como están. Los componentes nuevos los llaman; no los reemplazan.
- La semilla (`core/seed/`) no cambia. Los guiones de escenario dependen de ella.
