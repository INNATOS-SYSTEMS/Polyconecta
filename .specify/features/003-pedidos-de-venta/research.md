# Investigación: Pedidos de venta (F1)

Decisiones técnicas de la fase 1. El estado del código es el de la rama `003-pedidos-de-venta` en `539fdf7` (igual a `main` en `7c393a6` más la spec). Lo que dependa del SDK o de las tablas `adm*` cita `docs/contpaq/` (Principio VII); lo que todavía no se ha verificado se marca como supuesto.

---

## R-01 · Autenticación con usuarios propios (D-32)

**Decisión**:
- ASP.NET Core Identity **solo para credenciales**: `AddIdentityCore<CredencialUsuario>()` con `IdentityUserContext<CredencialUsuario, long>` (sin las tablas de roles de Identity) y `AddSignInManager`. Los grupos y permisos son del dominio (R-02), no de Identity.
- `CredencialUsuario` vive en `Infrastructure/Plataforma/Identidad/` y apunta 1:1 a la entidad de dominio `User` por `UserId`. El dominio no conoce Identity (CT-07).
- **Cookie de autenticación** (`HttpOnly`, `Secure` fuera de desarrollo, `SameSite=Lax`, expiración deslizante de 10 horas, un turno). Las peticiones de la API sin sesión responden `401` (no redirigen), y la web lleva a `/login?volver=<ruta>`.
- **Mismo origen**: la web llama a `/api` y `/hubs` por el proxy de desarrollo de Angular (`proxy.conf.json` hacia `:9020`). Así la cookie no necesita CORS con credenciales ni `SameSite=None`. En producción, el mismo origen lo da el proxy inverso del hosting (H-01).
- Rutas: `POST /api/v1/plataforma/sesion` (entrar), `DELETE /api/v1/plataforma/sesion` (salir), `GET /api/v1/plataforma/sesion` (usuario, grupos con planta y si es suplente, y permisos efectivos). La protección CSRF de la cookie es el encabezado `X-Requested-With` obligatorio en métodos que escriben, más `SameSite=Lax`.
- Contraseñas con la política de Identity por omisión (8 caracteres, mayúscula, número). El Administrador las asigna y las restablece (spec, supuesto 3). Bloqueo tras 5 intentos fallidos durante 15 minutos.
- `ICurrentUser` (ya existe en `Application/Common/Puertos.cs`) pasa a leer el usuario de la cookie: `UserId`, `UserName`, `NombreVisible` y el grupo ejercido de la acción en curso (R-02). Fuera de una petición (despachador y sincronización) sigue siendo "sistema".

**Paquete nuevo**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12, del mismo tren que EF Core 10.0.12 (CT-36). Se anota en "Exploración y cambios" antes de instalarlo (regla de autonomía 5).

**Alternativas**:
- JWT en `localStorage`: obliga a renovar tokens y expone el token a XSS. La aplicación es de una sola web, así que la cookie basta.
- Identity completo con `IdentityRole`: mezcla los roles de Identity con los grupos de D-148 y deja dos fuentes de verdad.
- SSO: D-32 lo deja para después.

---

## R-02 · Grupos, permisos y reglas de fila (D-148)

**Decisión**:
- **El catálogo de permisos lo define el código** en `Domain/Plataforma/Seguridad/Permisos.cs`: cada permiso es una clave estable `modulo.objeto.accion` (por ejemplo, `ventas.pedido.confirmar`) con su módulo, objeto (documento o funcionalidad), acción y etiquetas. Al arrancar, un sembrador sincroniza la tabla `plt.permission` con el catálogo: agrega lo nuevo y archiva lo que ya no existe. El árbol de los dos paneles sale de esas tres columnas.
- **Qué permisos tiene cada grupo es dato** (`plt.group_permission`). Los grupos iniciales son los diez de 01 §3, con la matriz de la capa 1 traducida a claves. Solo se siembran si la tabla está vacía; después los edita el Administrador.
- **Las firmas son dos permisos**, `ventas.pedido.firmar_comercial` y `ventas.pedido.firmar_cobranza`, detrás de un solo botón "Autorizar". Así qué grupo firma por qué rol es configurable sin código. El rol firmado se guarda en la firma.
- **Verificación en `Application`** (CT-11): cada petición de caso de uso que escribe implementa `IRequierePermiso { string Permiso { get; } }`, y un `AuthorizationDecorator` corre **antes** del de validación. Resuelve el **grupo ejercido**, que es el primero de las asignaciones activas del usuario con ese permiso (las de titular antes que las de suplente), y lo deja en `ICurrentUser.GrupoEjercido` para la bitácora, la auditoría y el chatter. Sin permiso lanza `PermisoDenegadoException`, que la API traduce a `403` con la razón.
- **Planta**: la asignación es usuario × grupo × planta. Si el documento tiene planta, solo cuentan las asignaciones de esa planta. El pedido no tiene planta (spec, supuesto 4), así que cuenta cualquiera.
- **Reglas de fila** (capa 2): `IReglaDeFila<T>` devuelve una `Expression<Func<T, bool>>` según el usuario. El servicio de consulta de listas (R-03) y los repositorios las aplican **antes** de cualquier filtro, así que un filtro no las amplía (02 §7). F1 deja el mecanismo y lo prueba con un documento de prueba con planta. Su primer uso real es la OF (RF-1, F2).
- **RF-3 y RF-4 son reglas del dominio**, no permisos: `SalesOrder.Firmar(usuario, rol, esSuplente)` rechaza un rol que ya firmó y a un usuario que ya firmó el pedido con cualquier rol.
- **Acciones disponibles con su razón** (CT-26): cada documento expone `AccionesDisponibles(usuario)` como una lista de `{ accion, disponible, razon }`. Combina el permiso (sin permiso: "Tu grupo no puede confirmar pedidos") y la precondición del dominio (por ejemplo, "Ya firmaste este pedido; la otra firma la da otra persona"). La API la devuelve en el detalle del documento y la interfaz solo la pinta.

**Alternativas**:
- Políticas de ASP.NET (`[Authorize(Policy=…)]`) en los controladores: ponen la regla en la capa HTTP y no sirven para el despachador ni las pruebas de aplicación.
- Permisos como texto libre en la base: no se pueden referenciar desde el código sin cadenas mágicas.

---

## R-03 · Consulta de listas por HTTP (resuelve P-28)

**Decisión** (ratificada con la observación de D-151):
- **Híbrida con umbral (D-151)**: `POST /api/v1/{modulo}/{lista}/conjunto` entrega el conjunto completo de la lista, con las reglas de fila aplicadas y los filtros con nombre de cada fila (`_filtros`), si tiene hasta 5,000 filas. Entonces `OrigenHttp<T>` resuelve búsqueda, filtros, agrupación, orden y página en el navegador, con la misma lógica que `OrigenEnMemoria` (que ya existe y tiene pruebas), sin nuevas consultas. Si no cabe, usa el modo servidor de abajo. Un cambio propio o volver a la lista recarga el conjunto.
- **Una ruta por lista**: `POST /api/v1/{modulo}/{lista}/consulta`, con el cuerpo igual a `ConsultaLista` (07 §4.1) en camelCase, y respuesta `ResultadoLista<T>`. Es `POST` porque la consulta lleva filtros anidados y la ruta de grupos, que en la URL quedan ilegibles y largos. No escribe nada. El detalle está en [contracts/api-listas.md](contracts/api-listas.md).
- **La vista de búsqueda se declara en el servidor** para las listas HTTP: `VistaDeBusqueda<T>` en `Application/Common/Listas/`, con campos buscables (`Expression<Func<T,string?>>`), filtros con nombre y su campo (`Expression<Func<T,bool>>`), agrupaciones (`Expression<Func<T,object>>` con etiqueta), columnas ordenables y sumables. `GET /api/v1/{modulo}/{lista}/vista` devuelve su descripción sin expresiones (nombres, campos y etiquetas). El panel de búsqueda de la web la pinta igual que hoy, y para las listas HTTP la vista TS de `core/search/views.ts` deja de ser la fuente.
- **Todo se traduce a `IQueryable`** con EF Core: reglas de fila → filtros con nombre (O dentro del campo, Y entre campos) → filtros por columna → búsqueda → orden → página. La agrupación usa `GroupBy` sobre la expresión del nivel pedido, con `Count` y `Sum` de las columnas sumables. Agrupada, la página es de grupos (D-140). Una prueba de integración verifica que cada consulta es **una** ida a SQL Server para filas o grupos, más una para el total.
- **`OrigenHttp<T>`** en `PolyConecta.Web/src/app/core/lista/origen-http.ts` implementa `OrigenDeLista<T>` con `HttpClient`: en modo conjunto delega en `OrigenEnMemoria` sobre las filas recibidas, con los filtros con nombre leídos de `_filtros`; en modo servidor manda la consulta. La pantalla no cambia: solo cambia el origen que le pasa su servicio de acciones (FR-029).
- **Exportar**: la exportación a `.xlsx` sigue en la web, con las filas que devuelve la consulta (con `ids` o con el filtro completo, en páginas de 200).

**Alternativas**:
- Solo servidor (D-135 original): cada filtro, agrupación u orden es una consulta. Se descartó en la ratificación por pedir una consulta por interacción en listas que caben de sobra en memoria.
- Solo navegador: deja sin salida a las listas que crezcan a decenas de miles de filas.
- OData: agrega una dependencia y un formato que no es el del contrato visual, y su agrupación con subtotales por niveles no cubre la paginación de grupos.
- GraphQL: excesivo para listas con una sola forma de consulta.
- `GET` con parámetros: los filtros `en` y `entre` y la ruta del grupo no caben bien en la URL.

---

## R-04 · Chatter guardado (D-78)

**Decisión**:
- `plt.chatter_message`: `document_type`, `document_id`, `kind` (`Mensaje`, `Nota`, `Cambio`), `body`, `author_user_id`, `author_name`, `group_exercised`, `created_at`. Solo se inserta.
- **Los registros de cambio los escribe la persistencia**: el `AuditoriaInterceptor`, que ya convierte `TransicionesPendientes` en `StateTransitionLog`, agrega en el mismo `SaveChanges` el mensaje `Cambio` ("Borrador → Confirmado", con la nota de la transición si la hay). Ningún caso de uso lo escribe a mano (escenario 5 de US4).
- **Tiempo real**: el hub `/hubs/chatter` pasa a `[Authorize]`. Cada cliente se une al grupo de SignalR del documento abierto (`{tipo}:{id}`), así que ya no se transmite a todos. `EnviarMensaje(documento, tipo, texto)` llama al caso de uso `PublicarMensaje` (que guarda con el autor de la sesión, no del cliente) y transmite al grupo. Los registros de cambio se transmiten después del `Commit`, con un `IChatterNotificador` que el decorador de transacción vacía al confirmar.
- **Documento**: el chatter del pedido usa `document_type = "ventas.pedido"` y su id. Las pantallas que siguen en memoria conservan el hub con su folio como hoy, pero sin guardar: un `document_type` que no está registrado en `IDocumentosConChatter` no se persiste. Se conectan en su fase.
- `GET /api/v1/plataforma/chatter/{tipo}/{id}` devuelve el historial, del más reciente al más antiguo, en páginas de 50.

**Alternativas**:
- Guardar desde el hub sin caso de uso: salta la autorización y la transacción.
- Escribir el registro de cambio en cada caso de uso: se olvida en alguno; el interceptor lo garantiza.

---

## R-05 · Sincronización de catálogos

**Decisión**:
- **Puerto** `IBridgeLecturas` en `Application/Plataforma/Erp/` (productos, clientes, almacenes y existencias, con cursor), implementado en `Infrastructure/Erp/BridgeLecturasHttp.cs` sobre el mismo `HttpClient` del bridge. Propaga el `correlation_id` (CT-31).
- **Caso de uso** `SincronizarCatalogo(catalogo)` por catálogo, y `SincronizarTodo`, que los corre en orden (almacenes, agentes, clientes y productos). Cada uno **lee el catálogo completo** por páginas de 500 (D-150: `CTIMESTAMP` no sirve para `modified_since`), hace upsert por id de CONTPAQi y guarda `erp_*` (CT-13). Solo escribe un registro si algún campo difiere. Un registro inactivo, o que ya no viene en la lectura completa, se archiva (FR-015); uno que vuelve a estar activo se restaura. Nunca borra. Si una corrida falla a la mitad, no archiva nada: el archivado por ausencia solo corre al terminar la lectura completa.
- **Estado por catálogo** en `plt.catalog_sync_state`: `catalogo`, `ultima_corrida`, `ultima_exitosa`, `ultimo_resultado` (`Exito` o `Error`), `registros_leidos`, `registros_cambiados`, `registros_archivados`, `duracion_ms` y `ultimo_error`. El upsert es idempotente: sin cambios en CONTPAQi, cero modificados (SC-004). Se mide la duración de la lectura completa en el VPS; si pasa de un minuto, se sube el intervalo.
- **Periódica** con un `BackgroundService` (`SincronizadorCatalogos`), cada `Erp:Sincronizacion:IntervaloMinutos` (15), y **bajo demanda** con `POST /api/v1/plataforma/sincronizacion/{catalogo}` y `POST /api/v1/plataforma/sincronizacion` (todos), que exigen el permiso `plataforma.sincronizacion.ejecutar` (Sistemas y Administrador).
- **Una instancia por catálogo**: `sp_getapplock` con el nombre `sync:<catalogo>`, igual que el despachador. Si está tomado, la petición manual responde `409` con "Ya hay una sincronización de productos en curso".
- **Lo que es de PolyConecta no se sobrescribe**: clasificación (solo se llena si está vacía), ficha técnica y el enlace ubicación ↔ almacén (CT-14).
- **El bridge simulado** se amplía con un catálogo semilla suficiente para R1: 5 clientes con moneda y domicilios, 20 productos (MP, rollos y PT, con KG, PZA y MIL) y los almacenes de las dos plantas. Es tarea de L1 (el simulador es común y lo cambian los dos líderes). Además, `PUT /admin/simulated/catalog/{products|clients}/{codigo}` cambia, agrega o quita un registro del catálogo semilla en caliente (activo, nombre, moneda, domicilios), para probar la comparación y el archivado sin reiniciar.

**Alternativas**:
- Leer CONTPAQi al momento en cada pantalla, sin copia: el pedido referencia productos y clientes con su id propio, y la lista necesita filtrar y agrupar en SQL Server.
- Sincronizar por eventos de CONTPAQi: no existen; la base no tiene disparadores propios y no se tocan las tablas `adm*` (Principio II).

---

## R-06 · Lecturas reales (L1, 1.4)

**Verificado por el usuario en CONTPAQi el 8-oct (D-150):**
- **`CTIMESTAMP` no es la fecha de última modificación** (`Referencia_BD_CONTPAQi.md` lo describe como "Concurrencia"). `modified_since` no se puede implementar en productos ni clientes: la sincronización lee completo y compara (R-05). La contingencia que aquí era la alternativa pasa a ser la decisión.
- **Moneda del cliente**: `admClientes` trae `CIDMONEDA` y `CIDMONEDA2`; la que tiene efecto es la **moneda del cliente, `CIDMONEDA`**. Se traduce a código ISO con la misma configuración que usa `ALTA_PEDIDO` (`BridgeConfig__Monedas__{ISO}`).
- **Domicilios**: `admDomicilios` con `CTIPOCATALOGO = 1` (clientes) y `CIDCATALOGO = CIDCLIENTEPROVEEDOR`. Un cliente tiene **un domicilio fiscal** (`CTIPODIRECCION = 0`) y **N de envío** (`CTIPODIRECCION = 1`). El contrato los lleva como lista.
- **Existencias** (FR-007): ya se leen como D-87 para un producto. 1.4 agrega varios productos por consulta con `IN` parametrizado, en lotes de 100.

**Alternativas**: buscar otra columna de fecha (`CFECHAALTA…` solo da el alta) o un disparador en las tablas `adm*` (prohibido, Principio II). Se descartan.

---

## R-07 · Sesión permanente del SDK (L1, 1.1)

**Decisión**:
- Hoy `ContpaqiSdkGateway` abre la empresa al haber trabajo y la cierra tras `IdleSessionTimeoutSeconds` (5 s por omisión). La secuencia de inicio ya es la de D-108. 1.1 cambia el ciclo de vida: **el SDK y los dos inicios de sesión se hacen una vez al arrancar** el proceso, y **la empresa se abre por lote de comandos** y se cierra al vaciarse la cola (D-91, CT-40). `IdleSessionTimeoutSeconds` deja de cerrar el SDK y solo cierra la empresa.
- **Tiempo límite por llamada** (FR-005): cada llamada nativa corre en el hilo STA del `OutboxWorker` con un vigilante. Si vence `BridgeConfig__Sdk__TimeoutSegundos` (60 por omisión), el bridge registra el bloqueo con su `correlation_id`, responde la transacción con `SDK_TIMEOUT` (ya está en el catálogo del contrato, reintentable; el reintento de un comando que escribe pasa por la reconciliación de CT-38) y **reinicia el proceso**: un hilo bloqueado dentro de la DLL no se puede abortar sin dejar el SDK inconsistente, y la tarea de D-115 lo vuelve a levantar.
- **Reinicio diario** (FR-006): `BridgeConfig__ReinicioDiario` (hora local, por omisión 03:00, dato de puesta en marcha D-98). A esa hora el bridge deja de tomar comandos, espera el actual, cierra empresa y SDK con `fTerminaSDK` y termina con código de salida 0. La tarea programada de D-115 lo reinicia. Se mide igual que SC-006 de F0.
- **Las lecturas no usan el SDK**: son SQL de solo lectura (CT-30). Por eso la sesión del SDK no afecta la sincronización de US3.
- La verificación de SC-006 de esta spec (una hora de lecturas y comandos simulados en modo real sin volver a iniciar sesión) usa la sonda que ya abre la empresa sin escribir (F0, `0.9.md`).

**Alternativas**: abortar el hilo bloqueado (`Thread.Interrupt`). No libera una llamada nativa y deja la DLL en estado desconocido.

---

## R-08 · Modelo del producto y convivencia con las entidades previas a F0

**Decisión**:
- El producto de F1 es una entidad nueva, `Product`, en `Domain/Inventario/` (tabla `inv.product`), con la base común, `erp_product_id`, `erp_code`, `erp_uom`, `tracks_lots` y `classification_id`. Sus unidades (`PackagingUnit`) guardan solo la base en F1 (`is_erp_base_unit = true`).
- La entidad previa `Domain/Entities/Product` (tabla `inv.Products`, id `Guid`) la usan `StockLot`, `ManufacturingOrder`, `Bom`, `QualityCheck` y `StockPicking`, que cada fase rediseña (05 §3). **Se queda sin cambios** y se marca `[Obsolete]` con el mensaje "Se retira cuando su fase rediseñe a quien la usa". La última fase que la use la borra. Así F1 no reescribe módulos fuera de su alcance.
- **Clasificación** (`inv.product_classification`): `code`, `name`, `erp_value` (el valor de CONTPAQi del que nació, si lo hay). La sincronización crea una clasificación por valor nuevo de CONTPAQi solo para llenar productos sin clasificación (FR-017).
- **Ficha técnica**: `RollSpecification` y `PtSpecification` en `inv`, 1:1 con el producto (04 §1, "delegación 1:1"). Los campos son los de 04 §3. Se guardan con la operación `GuardarFichaTecnica` del producto, que exige los dos bloques y liga el PT a su rollo (FR-018). El rollo ligado puede ser el del mismo producto o el de otro producto de 2.º proceso.

**Alternativas**: modificar `Entities/Product` y migrar sus `Guid`: obliga a tocar cinco entidades de otras fases y sus pruebas antes de rediseñarlas.

---

## R-09 · Pedido: edición con firmas, concurrencia y moneda

**Decisión**:
- **Editar con firmas (D-147)**: `PUT /api/v1/ventas/pedidos/{id}` lleva `revocarAutorizacion: bool` y la `rowVersion`. Si el pedido tiene firmas y el valor es `false`, la API responde `409` con el código `EDICION_REVOCA_AUTORIZACION` y el número de firmas, y la web muestra el aviso. Al confirmar, la web repite con `true`: el caso de uso `EditarPedido` aplica el cambio y llama a `SalesOrder.RevocarPorEdicion(usuario)`, que borra las firmas, deja Confirmado y registra la transición `Autorizado → Confirmado` (o `Confirmado → Confirmado` con nota) con la nota "Revocada por edición". El chatter guarda el cambio y la revocación (R-04).
- **Concurrencia**: el pedido ya tiene `RowVersion` (`AuditableEntity`). Un guardado o una firma con una versión vieja responde `409` `DOCUMENTO_MODIFICADO`, y la web recarga y avisa. Así dos firmas simultáneas del mismo rol no entran las dos (caso límite).
- **Moneda (D-146)**: el maestro guarda `currency` (ISO) y `exchange_rate`. "Nuevo" propone la moneda del cliente al elegirlo, y si no la tiene, la base (`Erp:MonedaBase`, `MXN`). Las monedas admitidas salen de `Erp:Monedas` (por omisión `MXN` y `USD`), igual que la configuración del bridge, para no aceptar en F1 una moneda que `ALTA_PEDIDO` rechazaría en F2. El tipo de cambio de la moneda base es 1 y no se edita.
- **Precio**: `decimal(18,6)` por la unidad base, igual que `admMovimientos.CPRECIO`. El total por línea (cantidad × precio) es solo informativo: IVA, descuentos y totales los calcula CONTPAQi (D-74).
- **Folio**: secuencia `PEDIDO_VENTA` con prefijo `PV-{yyyy}-`, relleno 4 y reinicio anual (`PV-2026-0001`), el mismo formato que la réplica usa para el pedido libre (05 §7.3).
- **Domicilio de entrega (D-149)**: el pedido guarda el id del domicilio elegido **y una copia del texto** al confirmar, para que un cambio posterior del domicilio en CONTPAQi no altere un pedido ya confirmado.

---

## R-10 · Pantallas que siguen en memoria y guiones de escenario

**Decisión**:
- Solo Pedidos y las pantallas nuevas (inicio de sesión, usuarios, grupos, productos, clientes y sincronización) usan la API. Las demás siguen sobre `core/state` (spec, último supuesto).
- **El flujo en memoria pierde su pedido**: la OF, la recolección y la entrega de la réplica cuelgan del pedido semilla `IV310-26` de `OperationalFlowState`. Ese pedido ya no estará en la lista de Pedidos. Su botón inteligente "Pedido" desde la OF se muestra **deshabilitado con la razón "Se conecta en F2"** (nunca con un origen falso, FR-014 de la spec 001). El estado en memoria del pedido semilla se conserva solo como dato interno de esas pantallas.
- **Guiones de escenario**: los pasos de Pedidos dejan de compararse con el prototipo, porque los datos ya no son los semilla. Se sustituyen por pruebas extremo a extremo con Playwright contra la API, con SQL Server en contenedor y el bridge simulado (CT-26, CT-27). Los guiones del resto de las pantallas siguen igual.
- **Componentes nuevos** que no tienen contrato visual: los **dos paneles de permisos** (`pc-odoo-dual-list`, árbol con mover acción o nodo, D-148) y el **formulario de inicio de sesión**. Entran primero a 07 y a la galería `/catalogo` con sus pruebas (CT-24). Usuarios, grupos, productos y clientes se componen con lista, formulario y pestañas, que ya tienen contrato.

---

## R-11 · Contrato `1.1` (FR-003)

**Decisión**, aprobada por los dos líderes el 8-oct: cambio compatible (§8 del contrato):

| Lectura | Campo nuevo, opcional | Origen en CONTPAQi |
| :--- | :--- | :--- |
| `GET /catalogs/products` | `clasificacion`: `{ codigo, nombre }` del valor de "TIPO DE PRODUCTOS" | `admProductos.CIDVALORCLASIFICACION{n}` → `admClasificacionesValores` (A-05). El número de la clasificación lo da la configuración |
| `GET /catalogs/clients` | `moneda`: código ISO | `admClientes.CIDMONEDA` (R-06, D-150), traducido con `BridgeConfig__Monedas__{ISO}` |
| `GET /catalogs/agents` (lectura nueva) | `codigo`, `nombre`, `tipo` (`venta`, `venta_cobro`, `cobro`), `id_erp`, paginada como las demás (D-153) | `admAgentes` (`CCODIGOAGENTE`, `CNOMBREAGENTE`, `CTIPOAGENTE`, `CIDAGENTE`) |
| `ALTA_PEDIDO` (F2) | `agente` opcional: código del agente (D-153) | `tDocumento.aCodigoAgente` → `admDocumentos.CIDAGENTE` |
| `GET /catalogs/clients` | `domicilios[]`: `id_erp`, `tipo` (`fiscal` o `envio`), `calle`, `numero_exterior`, `numero_interior`, `colonia`, `codigo_postal`, `ciudad`, `municipio`, `estado`, `pais`, `sucursal` | `admDomicilios` (R-06) |

Además, **`modified_since` se declara obsoleto** en productos y clientes: `CTIMESTAMP` no es una fecha de modificación (D-150), así que el bridge real no lo puede cumplir. Sigue respondiendo `501` si alguien lo manda, y PolyConecta no lo usa.

Los campos nuevos son opcionales: PolyConecta funciona sin ellos (FR-003). La suite de contrato agrega sus pruebas y los ejemplos van a `docs/contratos/ejemplos/`. Hasta la aprobación, L1 no los expone en el modo real y el simulador sí, marcados como `1.1` en el `_nota` de su semilla.
