# Contrato: componentes nuevos de `PolyConecta.Web`

Entradas y salidas de cada componente que construye la spec 011. Los tipos están en [data-model.md](../data-model.md). Todos son standalone, con prefijo `pc-` y carpeta en `src/app/shared/` (CT-09). El documento [`07-contratos-visuales.md`](../../../../docs/diseno/07-contratos-visuales.md) se escribe a partir de este, con anatomía, estados y capturas de la galería.

---

## `pc-odoo-list`: tabla de lista

| Entrada | Tipo | Obligatoria | Qué es |
| :--- | :--- | :---: | :--- |
| `lista` | `string` | Sí | Llave de la lista (`ventas.pedidos`) para favoritos y exportación |
| `origen` | `OrigenDeLista<T>` | Sí | De dónde salen las filas (data-model §1) |
| `columnas` | `ColumnaLista<T>[]` | Sí | `campo`, `titulo`, `tipo` (`texto`, `numero`, `moneda`, `fecha`, `estado`), `visible`, `ordenable`, `sumable`, `agrupable` y `ancho` |
| `seleccion` | `boolean` | No | Casillas por fila. Por omisión, verdadero |
| `acciones` | `AccionMasiva[]` | No | Acciones sobre la selección (`nombre`, `icono`, `ejecutar(ids)`). Exportar siempre está |
| `consultaInicial` | `Partial<ConsultaLista>` | No | Filtros y agrupación con los que abre |

| Salida | Cuándo |
| :--- | :--- |
| `abrir(fila)` | Clic en una fila: la página navega al formulario |
| `consultaCambio(consulta)` | Cambió orden, filtros, agrupación o página. La barra de búsqueda y el paginador la usan |

**Comportamientos**: los de la spec, "Contratos por escribir", Lista. **Nunca** ordena, filtra, agrupa ni pagina en el navegador.

---

## `pc-odoo-kanban`

| Entrada | Tipo | Obligatoria | Qué es |
| :--- | :--- | :---: | :--- |
| `origen` | `OrigenDeLista<T>` | Sí | El mismo de la lista |
| `campoEtapa` | `string` | Sí | Campo que agrupa en columnas (estado o, si no hay, otro) |
| `etapas` | `EtapaKanban[]` | Sí | Columnas en orden |
| `transiciones` | `TransicionKanban[]` | No | Vacío = no se puede arrastrar |
| `tarjeta` | `TemplateRef<{ $implicit: T }>` | Sí | Contenido de la tarjeta |

| Salida | Cuándo |
| :--- | :--- |
| `abrir(fila)` | Clic en una tarjeta |
| `movida({ fila, desde, hacia })` | La transición se ejecutó |
| `rechazada({ fila, motivo })` | La transición no procedió o se canceló su diálogo |

---

## `pc-odoo-many2one`: selección de registro

| Entrada | Tipo | Qué es |
| :--- | :--- | :--- |
| `origen` | `OrigenDeLista<T>` | De dónde busca, con `busqueda` |
| `aTexto` | `(r: T) => string` | Cómo se muestra |
| `limite` | `number` | Cuántas opciones muestra antes de "Buscar más…" (8) |
| `soloLectura`, `obligatorio` | `boolean` | |

Implementa `ControlValueAccessor`. "Buscar más…" abre un diálogo con `pc-odoo-list` en modo selección única.

---

## `pc-odoo-date`

`ControlValueAccessor` de `Date | null`. Entradas: `min`, `max`, `soloLectura`, `obligatorio`. Muestra la fecha con `dateStyle: 'medium'` en es-MX.

---

## `pc-odoo-number`

`ControlValueAccessor` de `number | null`. Entradas: `tipo` (`cantidad`, `moneda`, `porcentaje`), `decimales`, `unidad` (solo se muestra, D-127), `moneda` (ISO), `min` y `soloLectura`.

---

## `pc-odoo-tabs`, `pc-odoo-action-menu`, `pc-odoo-dialog`

- **`pc-odoo-tabs`:** pestañas de la hoja, sobre `BrnTabs`. Entrada: `pestanas: { id, titulo }[]`; el contenido va por `ng-template` con el id.
- **`pc-odoo-action-menu`:** el engranaje de acciones (solo ícono) que va donde terminan las migas del formulario, con su menú sobre el `Menu` del CDK. Entrada: `acciones: { nombre, icono, ejecutar, deshabilitada? }[]`.
- **`pc-odoo-dialog`:** marco de los diálogos (título, cuerpo, botones primario y secundario), sobre el `Dialog` del CDK. Lo usan las confirmaciones, el hard-stop y los diálogos de las transiciones.
- **`AvisosService`:** avisos flotantes de éxito, aviso y error, sobre el overlay del CDK. `exito(texto)`, `aviso(texto)` y `error(texto)`. Los dos primeros se cierran solos a los 4 s; el de error, solo con su botón.

---

## `pc-odoo-sync-status`: estado de sincronización (CT-15)

| Entrada | Tipo | Qué es |
| :--- | :--- | :--- |
| `estado` | `'NoAplica' \| 'Pendiente' \| 'Enviado' \| 'Confirmado' \| 'Error'` | |
| `folio`, `idErp` | `string \| null` | Lo que devolvió CONTPAQi |
| `error` | `{ codigo: string; mensaje: string } \| null` | |
| `puedeReintentar` | `boolean` | Solo Sistemas (D-93) |

| Salida | Cuándo |
| :--- | :--- |
| `reintentar()` | Sistemas pulsa "Reintentar" |

Solo en los documentos que envían un comando a CONTPAQi, arriba del chatter: un ícono por estado y un popover con el título y el detalle (contratos visuales §1.8). En esta spec se construye y se muestra en la galería con los cinco estados; ningún documento lo usa todavía, porque la réplica no sincroniza. F1 lo conecta.

---

## `pc-odoo-icon`

Entradas: `nombre` (del catálogo de PolyConecta, por ejemplo `confirmar`, `editar`, `quitar` o `imprimir`) y `contexto` (`boton` 16 px, `icono` 18 px, `inteligente` 15 px). Pinta el ícono Lucide equivalente (research R-07).

---

## Botones

No hay componente de botón: se siguen usando `btn btn-primary` y `btn btn-outline-secondary`, ya con la variante A (E3). Los botones de ícono usan `btn o_btn_icon`, con el hover `--btn-icon-hover-bg`.
