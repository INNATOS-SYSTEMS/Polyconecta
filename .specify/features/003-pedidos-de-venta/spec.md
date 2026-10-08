# Feature Specification: Pedidos de venta (F1)

**Feature Branch**: `003-pedidos-de-venta`

**Created**: 2026-10-05 · **Completada**: 2026-10-08

**Status**: Completa, por ratificar con los dos líderes antes de que empiece la fase (12-oct). El alcance del "Diseño sin tarea en el plan" lo decidió el usuario el 8-oct (D-145).

**Fase del plan**: F1 · Pedidos de venta, 12 – 19 oct. Termina en la revisión **R1** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 1.1 a 1.7 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F1 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-15, D-32 a D-38 (roles, firmas y suplentes), D-52, D-53, D-74, D-78, D-86, D-87, D-88, D-91, D-93, D-108, D-113, D-123, D-124, D-127, D-116 a D-120 (fases y specs), D-134 a D-144 (contratos visuales), D-145 (alcance de F1), D-146 (moneda del pedido), D-147 (editar con firmas), D-148 (grupos y permisos) y D-149 (domicilios del cliente).

---

## Contexto para el agente

- **Objetivo de la fase.** Atención a Clientes captura un pedido libre en PolyConecta, lo confirma, y lo autorizan Comercial y Cobranza con dos firmas de personas distintas. Para eso la fase entrega usuarios, roles y permisos por planta, la sincronización de productos, clientes y almacenes, la ficha técnica y la clasificación propia del producto, la sesión permanente del bridge con sus lecturas, y la búsqueda, los favoritos y el chatter que usarán todas las pantallas.
- **Diseño que la sostiene.** [02 §0 y §1](../../../docs/diseno/02-flujo-y-reglas.md) (documento libre, pedido, estados y dos firmas), [02 §6](../../../docs/diseno/02-flujo-y-reglas.md) (unidades), [02 §7](../../../docs/diseno/02-flujo-y-reglas.md) (interfaz), [01 §3](../../../docs/diseno/01-modulos-y-roles.md) (roles, matriz y reglas de fila), [04 §3](../../../docs/diseno/04-modelo-de-dominio.md) (Catálogo de productos, Comercial, Seguridad y auditoría, Configuración de interfaz), [07](../../../docs/diseno/07-contratos-visuales.md) (contratos visuales), CT-13, CT-14, CT-24 a CT-26, CT-40.
- **Contrato.** Lecturas `GET /catalogs/products`, `/catalogs/clients`, `/catalogs/warehouses` e `/inventory/stocks` (§6 de [`bridge-v1.md`](../../../docs/contratos/bridge-v1.md)). Ningún comando de escritura: `ALTA_PEDIDO` es de F2. Propuesta de cambio compatible (`1.1`): la clasificación en la lectura de productos, y la moneda y los domicilios en la de clientes (FR-003).
- **Interfaz.** Las pantallas de Pedidos (lista, kanban, formulario y "Nuevo") ya existen en `PolyConecta.Web` sobre estado en memoria, armadas con los contratos visuales (spec 011). F1 las conecta a la API sin cambiar su estructura: la lista cambia `OrigenEnMemoria` por `OrigenHttp` (07 §4.1). Las pantallas nuevas (inicio de sesión, usuarios, productos, clientes) se componen con los mismos contratos (CT-24).
- **Depende de.** F0: contrato `bridge-v1`, base común (auditoría, bitácora, folios y outbox), simulador, aplicación web y CI. Spec 011: contratos visuales.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F1. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 1.1 | Sesión permanente con CONTPAQi: doble inicio de sesión y tiempos límite | L1 | 12 – 13 oct | 12 |  |
| 1.2 | Usuarios, roles y permisos por planta | L2 | 12 – 14 oct | 9 |  |
| 1.3 | Sincronizar productos, clientes y almacenes de CONTPAQi | L2 | 12 – 14 oct | 9 | Incluye clasificación y ficha técnica (D-145) |
| 1.4 | Lectura de catálogos y existencias de CONTPAQi | L1 | 13 – 15 oct | 16 | Incluye el cotejo de T-06 (D-145) |
| 1.5 | Pedido de venta: captura, confirmación y autorización con dos firmas | L2 | 13 – 16 oct | 13 | Incluye la revocación (D-145) |
| 1.6 | Búsqueda, filtros y conversación por documento en listas y formularios | L2 | 15 – 19 oct | 12 |  |
| 1.7 | Preparar la revisión R1 | L2 | 19 oct | 4 |  |

Horas: L1 28 · L2 47 · Común 0. Lo que D-145 agrega (revocación, clasificación, ficha técnica y cotejo) no tiene horas en el plan; si no cabe, se registra en "Exploración y cambios" con lo que se mueve.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | El motor de abastecimiento: al autorizar solo cambia el estado; el motor se conecta en F2 |
| Pedido libre ("Nuevo") con precio unitario por línea, y moneda, tipo de cambio y domicilio de entrega en el maestro | `ALTA_PEDIDO` y el estado de sincronización del pedido (F2). El ícono de sincronización del pedido muestra `No aplica` hasta entonces |
| Revocación de la autorización, sin documentos generados que descartar (D-33, D-145) | Descartar documentos generados al revocar: se agrega en F2, con el motor |
| Clasificación propia del producto (D-86) y ficha técnica, bloques Rollo y PT (D-145) | **Pedido capturado en CONTPAQi que entra por sincronización** (D-53): fuera de alcance del proyecto por ahora (D-145) |
| Cotejo de F-01, F-02 y F-05 con la UI de CONTPAQi, junto con 1.4 (T-06, D-145) | Tablero de sincronización (2.7, F2) |
| Favoritos por usuario en la base y chatter guardado en la base (D-78) | Las pantallas de los otros módulos siguen sobre estado en memoria; se conectan en su fase |

### Preguntas abiertas y decisiones por validar

- **T-06** (frescura de la lectura de existencias, tarea 1.4): se especifica con la lectura directa de D-87 como supuesto. El cotejo de esta fase la cierra o la deja con su resultado registrado.
- **P-28** (contrato HTTP de la consulta de listas): se fija en el plan de esta fase con la lista de Pedidos, la primera que lee de la API, y pasa a `docs/diseno/` al cerrar.
- **D-146**: moneda y tipo de cambio por pedido, no por línea, como ya lo fija `ALTA_PEDIDO` en el contrato `1.0`. La moneda se propone con la del cliente y AC la puede cambiar en el maestro.
- **P-25** (de dónde salen los kg): la meta de producción por línea se captura como dato; su uso en cálculos es de F2 en adelante.

### Puesta en marcha

Titulares y suplentes de cada rol, con su planta (ex P-02). No bloquea la construcción (D-75): la fase trabaja con usuarios de ejemplo y la operación los entrega antes del piloto.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pedido libre: captura, confirmación y autorización con dos firmas (Priority: P1) · L2 (1.5)

Atención a Clientes crea un pedido con "Nuevo", elige el cliente, captura sus líneas con cantidad en la unidad base del producto y su precio, y lo confirma. Comercial y Cobranza lo autorizan, cada uno con su propia firma; la segunda firma lo deja Autorizado. Cualquiera de los firmantes o el Administrador puede revocar la autorización.

**Why this priority**: es el objetivo de la fase y lo que revisa la operación en R1. Todo lo demás de F1 existe para sostenerlo.

**Independent Test**: con usuarios de ejemplo de AC, Comercial y Cobranza, un pedido libre se crea, se confirma, recibe las dos firmas y queda Autorizado; recargar la página no pierde nada, y la bitácora y el chatter muestran quién hizo cada paso y con qué rol.

**Acceptance Scenarios**:

1. **Given** AC en la lista de Pedidos, **When** pulsa "Nuevo", elige un cliente sincronizado y agrega una línea con un producto, **Then** la unidad de la línea es la unidad base del producto en CONTPAQi y no se puede cambiar (D-127), y el pedido se guarda en Borrador con su folio de `IReferenceSequenceService`.
2. **Given** un pedido en Borrador sin líneas, o con una línea sin cantidad o sin precio, **When** AC pulsa "Confirmar", **Then** no se confirma y el formulario dice qué falta.
3. **Given** un pedido en Borrador completo, **When** AC lo confirma, **Then** pasa a Confirmado y la transición queda en `StateTransitionLog` y en el chatter con usuario, rol y fecha.
4. **Given** un pedido Confirmado, **When** un usuario con el rol Comercial pulsa "Autorizar", **Then** se registra su firma, el pedido sigue en Confirmado y el formulario muestra "1 de 2 firmas" y cuál falta.
5. **Given** un pedido con la firma de Comercial, **When** un usuario distinto con el rol Cobranza pulsa "Autorizar", **Then** el pedido pasa a Autorizado.
6. **Given** un usuario con los grupos Comercial y Cobranza que ya firmó un pedido, **When** intenta dar la segunda firma, **Then** "Autorizar" aparece deshabilitado con la razón "Ya firmaste este pedido; la otra firma la da otra persona" (RF-4, D-34).
7. **Given** el suplente designado de Cobranza, **When** firma, **Then** la firma vale como la de Cobranza y queda atribuida a él, con el rol ejercido (D-38).
8. **Given** un pedido con una o dos firmas, **When** uno de sus firmantes o el Administrador pulsa "Revocar autorización" con un motivo, **Then** las firmas se borran, el pedido regresa a Confirmado y el motivo queda en el chatter (D-33).
9. **Given** un pedido Confirmado o Autorizado, **When** AC o el Administrador lo cancela con un motivo, **Then** pasa a Cancelado y ya no se edita.
10. **Given** el kanban de Pedidos, **When** AC arrastra una tarjeta de Borrador a Confirmado, o Comercial de Confirmado a Autorizado, **Then** se ejecuta la misma acción que el botón del formulario, con su diálogo de firma (D-138); si no procede, la tarjeta regresa con el motivo.

---

### User Story 2 - Usuarios, grupos y permisos por planta, con suplentes (Priority: P1) · L2 (1.2)

Cada persona entra con su usuario y contraseña de PolyConecta. Los **grupos** son un catálogo: cada uno trae un conjunto de permisos ya armado que el Administrador puede modificar, y un área con varios niveles (por ejemplo, Producción) tiene un grupo por nivel. El Administrador da de alta usuarios y les asigna sus grupos por planta, como titular o suplente. Lo que un usuario no puede hacer se ve deshabilitado, con su razón (D-148).

**Why this priority**: sin identidad no hay firmas de personas distintas ni atribución, y la regla central del pedido (RF-4) no se puede cumplir.

**Independent Test**: el Administrador crea un usuario, le asigna el grupo Comercial en PIM como suplente, y ese usuario entra, ve el pedido y puede firmar como Comercial; otro usuario sin grupo de firma ve "Autorizar" deshabilitado con su razón, y la API rechaza su firma aunque la mande directo. Al quitarle al grupo Comercial el permiso de firmar, ningún miembro puede firmar, sin tocar código.

**Acceptance Scenarios**:

1. **Given** un usuario activo, **When** entra con su usuario y contraseña, **Then** la barra superior muestra su nombre y su inicial, y "Cerrar sesión" termina la sesión (D-143).
2. **Given** un usuario sin sesión, **When** abre cualquier ruta, **Then** se le pide iniciar sesión y, al entrar, vuelve a la ruta que pidió.
3. **Given** el Administrador, **When** crea un usuario y le asigna uno o varios grupos, cada uno con su planta y la marca de suplente, **Then** el usuario entra con los permisos de esos grupos. Un usuario activo tiene al menos un grupo.
4. **Given** un usuario cuyos grupos no tienen el permiso de una acción, **When** el usuario abre el documento, **Then** la acción aparece deshabilitada con su razón (CT-26); **When** la pide directo a la API, **Then** la API la rechaza con `403` y la misma razón.
5. **Given** un usuario archivado, **When** intenta entrar, **Then** no puede; sus firmas y transiciones anteriores siguen atribuidas a él.
6. **Given** un grupo, **When** el Administrador abre sus permisos, **Then** los ve en dos paneles, a la izquierda los disponibles y a la derecha los asignados, los dos agrupados en árbol por módulo, documento o funcionalidad, y acción; pasa de un lado al otro una acción o un nodo completo (todo un documento o todo un módulo) para asignar o quitar, y al guardar el cambio aplica a todos sus miembros sin tocar código de negocio (01 §3).
7. **Given** el catálogo de grupos, **When** el Administrador crea un grupo nuevo (por ejemplo, un nivel más de Producción), **Then** puede partir de un grupo existente para copiar sus permisos y ajustarlos.

---

### User Story 3 - Catálogos de CONTPAQi sincronizados, con clasificación y ficha técnica (Priority: P1) · L2 (1.3)

Productos, clientes y almacenes llegan de CONTPAQi por el bridge y se mantienen al día solos. PolyConecta agrega lo que es suyo: la clasificación del producto y la ficha técnica (bloques Rollo y PT).

**Why this priority**: el pedido no se puede capturar sin clientes ni productos con su unidad base.

**Independent Test**: contra el simulador, la primera sincronización trae todo el catálogo semilla; un cambio en un producto del simulador aparece en PolyConecta en la siguiente sincronización sin volver a traer lo que no cambió; un producto inactivo ya no se ofrece en una línea nueva.

**Acceptance Scenarios**:

1. **Given** una base vacía, **When** corre la primera sincronización, **Then** quedan en PolyConecta todos los productos (con clave, nombre, unidad base, si lleva lote y si está activo), clientes (con su moneda, su domicilio fiscal y sus domicilios de envío) y almacenes que lee el bridge, con su `erp_*` (CT-13).
2. **Given** una sincronización previa, **When** corre la siguiente, **Then** lee el catálogo completo y solo escribe lo que cambió; un registro que ya no viene de CONTPAQi se archiva (D-150).
3. **Given** un producto que pasa a inactivo en CONTPAQi, **When** se sincroniza, **Then** se archiva: no aparece al capturar una línea nueva y los pedidos que ya lo tienen no cambian.
4. **Given** el bridge caído, **When** toca sincronizar, **Then** la sincronización queda en error con su motivo, se reintenta en la siguiente vuelta y no borra ni archiva nada de lo ya sincronizado.
5. **Given** un producto nuevo, **When** se sincroniza, **Then** su clasificación propia toma como valor inicial la de CONTPAQi, si el contrato la trae (FR-003); **When** el Administrador la cambia en PolyConecta, **Then** una sincronización posterior no la sobrescribe (CT-14, D-86).
6. **Given** un producto terminado, **When** AC abre su ficha técnica, **Then** captura los bloques Rollo y PT (04 §3), siempre los dos, y el bloque PT queda ligado a su bloque Rollo.
7. **Given** Sistemas o el Administrador, **When** pulsa "Sincronizar ahora" en un catálogo (productos, clientes o almacenes) o en general (los tres), **Then** la sincronización corre de inmediato y muestra, por catálogo, cuándo terminó y cuántos registros trajo.
8. **Given** un almacén de CONTPAQi, **When** se sincroniza, **Then** queda disponible para ligarlo a una ubicación de PolyConecta (`erp_warehouse_id`); la sincronización no crea ubicaciones (D-43, CT-14).

---

### User Story 4 - Búsqueda, filtros, agrupaciones, favoritos y chatter en la base (Priority: P2) · L2 (1.6)

Las listas que leen de la API buscan, filtran, agrupan, ordenan y paginan en el servidor, con la misma vista de búsqueda declarativa que ya usa la interfaz. Los favoritos se guardan por usuario. El chatter de cada documento se guarda en la base, con mensajes, notas internas y el registro automático de cada cambio de estado.

**Why this priority**: es lo que hace usable la lista de pedidos con datos reales y lo que heredan todas las fases siguientes; el pedido se puede probar sin ello.

**Independent Test**: con 500 pedidos de ejemplo, la lista de Pedidos filtra, agrupa por estado y por cliente, ordena y pagina, y cada consulta llega a la API (ninguna se resuelve en el navegador); un favorito guardado por un usuario no lo ve otro; un mensaje del chatter sigue ahí al recargar y lo ve otro usuario en vivo.

**Acceptance Scenarios**:

1. **Given** la lista de Pedidos, **When** el usuario combina dos filtros del mismo campo y uno de otro campo, **Then** los del mismo campo se unen con O y los de campos distintos se cruzan con Y, y el filtro queda en la URL (02 §7).
2. **Given** una lista agrupada por cliente, **When** abre un grupo, **Then** ve su conteo y subtotales, y el paginador pagina grupos (D-140).
3. **Given** un usuario, **When** guarda un favorito y lo marca por omisión, **Then** se aplica al abrir la lista con su sesión, en cualquier navegador, y otro usuario no lo ve.
4. **Given** un documento, **When** el usuario escribe un mensaje o una nota interna, **Then** se guarda con autor y fecha, aparece en vivo a quien tenga el documento abierto y sigue ahí al recargar (D-78).
5. **Given** una transición de estado, **When** se ejecuta, **Then** el chatter registra solo "Borrador → Confirmado" con usuario y rol, sin que nadie lo escriba.
6. **Given** una regla de fila que restringe lo que ve un grupo, **When** el usuario quita todos los filtros, **Then** sigue sin ver lo que la regla le oculta (02 §7).

---

### User Story 5 - Sesión permanente del bridge y lecturas de catálogos y existencias (Priority: P1) · L1 (1.1, 1.4)

El bridge real abre la sesión del SDK una sola vez, con los dos inicios de sesión, y la mantiene; cada llamada tiene tiempo límite. Sus lecturas cumplen el §6 del contrato: paginación, unidad base y lote del producto, moneda y domicilios del cliente, y existencias de varios productos por consulta. El cotejo con la UI de CONTPAQi confirma que la lectura directa es fiel.

**Why this priority**: la sincronización de US3 depende de estas lecturas, y la sesión permanente es requisito de todos los comandos de F2 en adelante (D-91, D-108).

**Independent Test**: en el VPS, el bridge arranca, abre la sesión una vez, atiende lecturas durante una hora sin volver a iniciar sesión y sin ventanas de ingreso; la suite de contrato de lecturas pasa contra el real y contra el simulador.

**Acceptance Scenarios**:

1. **Given** el bridge real al arrancar, **When** inicia el SDK, **Then** inicia sesión con el usuario de Comercial antes de `fSetNombrePAQ` y con el usuario centralizado después, con credenciales de variables de entorno, sin que aparezca ninguna ventana (CT-40, D-108).
2. **Given** una llamada al SDK que no responde, **When** vence su tiempo límite, **Then** el bridge la corta, la registra con su `correlation_id` y responde con el error del contrato, sin quedarse bloqueado.
3. **Given** la ventana diaria configurada, **When** llega, **Then** el bridge cierra empresa y SDK y vuelve a iniciar solo (D-91).
4. **Given** `GET /catalogs/products?limit=…&cursor=…`, **When** se pide, **Then** responde una página en `snake_case` con cursor, unidad base (`CABREVIATURA`), si lleva lote y si está activo.
5. **Given** `GET /inventory/stocks` con varios productos, **When** se pide, **Then** responde la existencia por producto y almacén (F-01) y por lote (F-02), con la unidad base.
6. **Given** el cotejo de T-06, **When** se cambia una existencia en la UI de CONTPAQi del VPS, **Then** se mide cuándo la refleja la lectura (F-05), y F-01 y F-02 se comparan con lo que muestra la UI; el resultado va a la matriz y a `preguntas-abiertas.md` o `decisiones.md`.

---

### Edge Cases

- Dos usuarios autorizan el mismo pedido al mismo tiempo con el mismo rol: solo una firma entra; la otra recibe "Ya está firmado por Comercial" (control de concurrencia, `rowversion`).
- Un usuario pierde el rol entre que abre el pedido y pulsa "Autorizar": la API rechaza la firma con `403`; la pantalla no decide.
- Un producto se archiva por la sincronización mientras un pedido en Borrador lo tiene: el pedido lo conserva y se puede confirmar; una línea nueva ya no lo ofrece.
- El usuario que firmó se archiva antes de la segunda firma: su firma sigue valiendo y atribuida a él.
- Un pedido se edita en otra pestaña después de la primera firma: al guardar, la API detecta que tiene firmas y pide la confirmación de D-147; sin ella no guarda.
- La sincronización corre dos veces a la vez (dos instancias de la API): solo una procesa (bloqueo de aplicación, como el despachador).
- El bridge reinicia en su ventana diaria mientras corre una sincronización: la lectura falla, se reintenta en la siguiente vuelta y no se archiva nada, porque el archivado por ausencia solo corre al terminar la lectura completa.
- Una moneda distinta de la base sin tipo de cambio: el pedido no se confirma.
- La sesión del usuario vence con un formulario abierto: al guardar se le pide entrar otra vez y no se pierde la captura.

---

## Requirements *(mandatory)*

### Functional Requirements

**Común · contrato**

- **FR-001**: Las lecturas de §6 del contrato (`products`, `clients`, `warehouses` e `inventory/stocks`) MUST cumplir lo que el contrato `1.0` marca para F1: paginación con `limit` y `cursor`, unidad base y lote en productos, varios productos por consulta en existencias y nombres en `snake_case`.
- **FR-002**: La suite de contrato (CT-23) MUST cubrir esas lecturas y pasar contra el simulador en CI y contra el bridge real en el VPS.
- **FR-003**: Se propone el contrato `1.1` (cambio compatible, §8 del contrato) con tres campos opcionales: `clasificacion` en `GET /catalogs/products`, el valor de la clasificación "TIPO DE PRODUCTOS" de CONTPAQi (A-05); y en `GET /catalogs/clients`, `moneda` (código ISO de la moneda por omisión del cliente, D-146) y `domicilios[]` (los de `admDomicilios` del cliente: tipo fiscal o envío, calle, números, colonia, código postal, ciudad, municipio, estado, país y sucursal, D-149). Lo aprueban los dos líderes (CT-22). Sin `clasificacion`, la clasificación propia empieza vacía; sin `moneda`, el pedido propone la moneda base; sin `domicilios`, el pedido no ofrece domicilio de entrega.

**L1 · Integración (1.1, 1.4)**

- **FR-004**: El bridge real MUST iniciar el SDK una sola vez por proceso, con los dos inicios de sesión de D-108 y credenciales en variables de entorno (CT-29, CT-40), y mantener la sesión abierta mientras el proceso viva, sustituyendo la apertura y el cierre por inactividad de hoy (05 §4).
- **FR-005**: Toda llamada al SDK MUST tener tiempo límite configurable; al vencer, el bridge MUST registrarla con su `correlation_id` y responder con el error del contrato.
- **FR-006**: El bridge MUST reiniciarse en una ventana diaria configurable, cerrando siempre empresa y SDK (D-91, H-7).
- **FR-007**: Las lecturas reales MUST salir por SQL de solo lectura (CT-30), con las columnas documentadas en `docs/contpaq/` (Principio VII): existencia por producto y almacén de `admExistenciaCosto` del ejercicio vigente, y por lote de `admCapasProducto` agrupada por número de lote (D-87).
- **FR-008**: L1 MUST ejecutar F-05 y cotejar F-01 y F-02 con la UI de CONTPAQi en el VPS (T-06), registrar el resultado en la matriz del SDK y llevar la conclusión a `decisiones.md` o a `preguntas-abiertas.md`. La moneda del cliente, los domicilios y `CTIMESTAMP` ya los verificó el usuario el 8-oct (D-150).

**L2 · PolyConecta**

*Identidad y permisos (1.2)*

- **FR-009**: PolyConecta MUST autenticar con usuarios propios (ASP.NET Identity, D-32). Toda ruta de la API, salvo el inicio de sesión y el callback del bridge, MUST exigir sesión.
- **FR-010**: MUST existir `User`, `Group` (catálogo de grupos), `GroupAssignment` (usuario × grupo × planta, con marca de suplente), `Permission` (módulo › documento o funcionalidad › acción, catálogo cerrado que define el código) y su asignación a grupos, y `RecordRule` (04 §3, D-148). Los datos iniciales son los diez roles de 01 §3 como grupos, con la matriz de la capa 1 como sus permisos. Un usuario activo MUST tener al menos un grupo.
- **FR-011**: Cada caso de uso MUST verificar el permiso de la acción y las reglas de fila en `Application`, no en la interfaz ni en el controlador (CT-11). La API MUST devolver, por documento, las acciones disponibles y la razón de las que no lo están, para mostrarlas deshabilitadas (CT-26).
- **FR-012**: `ICurrentUser` MUST dar el usuario de la sesión y el grupo con el que actúa (el "rol ejercido" de CT-32); la auditoría, `StateTransitionLog` y el chatter MUST guardar los dos (CT-32).
- **FR-013**: El Administrador MUST poder crear, editar y archivar usuarios, asignarles grupos por planta como titular o suplente y restablecer su contraseña; y crear, copiar, editar y archivar grupos y asignarles permisos con **dos paneles con selección de izquierda a derecha** (disponibles y asignados) para asignar o quitar, los dos agrupados en árbol **Módulo › Documento o funcionalidad › Acción**, donde se puede mover una acción o un nodo completo (D-148). "Funcionalidad" es lo que no es un documento: sincronización, catálogos, ficha técnica, usuarios y grupos. Todo con pantallas armadas con los contratos visuales; los dos paneles son un componente nuevo y entran primero a 07 y a la galería `/catalogo` (CT-24).

*Catálogos (1.3)*

- **FR-014**: Una sincronización periódica (cada 15 minutos por omisión, configurable) y bajo demanda ("Sincronizar ahora" por catálogo y en general, Sistemas y Administrador) MUST leer completos productos, clientes y almacenes por el bridge, escribir solo lo que cambió, archivar lo que ya no viene, guardar su `erp_*` (CT-13) y el resultado de la última corrida por catálogo, y correr una sola instancia por catálogo a la vez.
- **FR-015**: Lo sincronizado MUST ser de solo lectura en PolyConecta, salvo lo que es de PolyConecta (CT-14). Un producto o cliente inactivo en CONTPAQi MUST archivarse, no borrarse.
- **FR-016**: `Product` MUST guardar su unidad base de CONTPAQi (`erp_uom`) y si lleva lote; `PackagingUnit` MUST reflejar la unidad base (`is_erp_base_unit`), sin conversión (D-124, D-127).
- **FR-017**: `ProductClassification` MUST ser un catálogo de PolyConecta, editable por el Administrador; la sincronización solo llena la clasificación de un producto que aún no tiene (D-86, FR-003).
- **FR-018**: La ficha técnica MUST capturarse en PolyConecta por producto, con los bloques `RollSpecification` y `PtSpecification` de 04 §3, siempre los dos y el PT ligado a su rollo. La captura AC y el Administrador; los demás roles la leen.

*Pedido de venta (1.5)*

- **FR-019**: `SalesOrder` y `SalesOrderLine` MUST seguir 04 §3 con `origin = Manual`, heredar la base común y cambiar de estado solo por transiciones nombradas (CT-32). Sus estados son los de 02 §1; en F1 se usan Borrador, Confirmado, Autorizado y Cancelado.
- **FR-020**: El maestro MUST llevar cliente (sincronizado y activo), orden de compra del cliente, agente, fecha del pedido, fecha estimada de entrega (`promise_date`, D-140), domicilio de entrega elegido entre los domicilios de envío del cliente (propone el único si tiene uno; si tiene varios, AC elige, D-149, D-150), moneda (propone la del cliente y AC la puede cambiar) y tipo de cambio (D-146). Cada línea MUST llevar producto activo, cantidad en la unidad base del producto (no editable, D-127), **precio unitario como columna capturable de la captura de líneas** (`[Producto] [Cantidad] [Unidad] [Precio unitario] [Agregar]`, D-74), y meta de producción y tolerancia opcionales (02 §1). El precio es por la unidad base, que es la que viaja a CONTPAQi (`admMovimientos.CPRECIO`).
- **FR-021**: El folio MUST salir de `IReferenceSequenceService`, con un tipo de documento propio del pedido de venta.
- **FR-022**: "Confirmar" (AC, Administrador) MUST exigir cliente, al menos una línea y, en cada línea, cantidad mayor que cero y precio; y tipo de cambio si la moneda no es la base.
- **FR-023**: "Autorizar" MUST ser un solo botón para Comercial y Cobranza que registra la firma del grupo (Comercial o Cobranza) con el que actúa el usuario (titular o suplente). Un grupo no firma dos veces, un grupo sin el permiso de firmar no firma, y **ninguna persona aporta las dos firmas del mismo pedido** (RF-3, RF-4, D-34). La segunda firma pasa el pedido a Autorizado.
- **FR-024**: El maestro y las líneas MUST editarse en Borrador y en Confirmado (AC y Administrador). Si el pedido ya tiene firmas, la interfaz MUST avisar antes de guardar que el cambio revoca la autorización, y al confirmarlo la API MUST guardar el cambio y revocar en la misma transacción: borra las firmas, regresa el pedido a Confirmado y deja en el chatter el cambio y la revocación automática, con usuario y rol (D-147). Sin la confirmación, la API MUST rechazar el guardado. Desde F2, si la revocación no procede (D-33), la edición tampoco.
- **FR-025**: "Revocar autorización" (firmantes del pedido y Administrador) MUST exigir motivo, borrar las firmas y regresar el pedido a Confirmado (D-33). En F1 no hay documentos generados; F2 agrega la verificación de que ninguno haya avanzado.
- **FR-026**: "Cancelar" (AC y Administrador) MUST exigir motivo y proceder desde Borrador, Confirmado o Autorizado. Un pedido Cancelado no se edita.
- **FR-027**: La lista, el kanban, el formulario y "Nuevo" de Pedidos que ya existen en `PolyConecta.Web` MUST leer y escribir por la API, sin cambiar su estructura (07); las transiciones del kanban MUST llamar a las mismas acciones que los botones (D-138).

*Búsqueda, favoritos y chatter (1.6)*

- **FR-028**: La API MUST resolver la consulta de lista de 07 §4.1 (página, orden, filtros, filtros con nombre, búsqueda, agrupación por niveles con conteo y subtotales, e ids) en el servidor, sobre la vista de búsqueda declarativa del modelo, y aplicar las reglas de fila antes que cualquier filtro. El formato HTTP se fija en el plan y resuelve P-28.
- **FR-029**: `OrigenHttp<T>` MUST implementar `OrigenDeLista<T>` contra esa API; la lista de Pedidos y las de Usuarios, Productos y Clientes MUST usarlo.
- **FR-030**: Los favoritos (`SavedSearch`) MUST guardarse por usuario y lista en la base, con uno por omisión por lista (07 §4.2), sustituyendo `FavoritosEnNavegador`.
- **FR-031**: El chatter MUST guardarse en la base por documento (`ChatterMessage`): mensaje, nota interna o registro de cambio de estado, con autor, rol y fecha (D-78). Cada transición MUST escribir su registro en el mismo `SaveChanges`. El hub `/hubs/chatter` MUST exigir sesión y tomar el autor de ella, no del cliente.

*Revisión (1.7)*

- **FR-032**: MUST existir datos de ejemplo para R1 (usuarios por rol, incluido un suplente y una persona con Comercial y Cobranza, y catálogos del simulador) y un guion de la revisión que recorra US1 a US4.

### Key Entities

- **`User`, `Group`, `GroupAssignment`, `Permission`, `RecordRule`**: identidad y permisos (04 §3, 01 §3, D-148). Los grupos son un catálogo con permisos configurables; la asignación lleva planta y marca de suplente.
- **`Customer`**, **`CustomerAddress`**: cliente sincronizado de CONTPAQi, de solo lectura, con su moneda por omisión y sus domicilios fiscal y de envío (D-149).
- **`Product`**, **`PackagingUnit`**: producto sincronizado con su unidad base y si lleva lote.
- **`ProductClassification`**: clasificación de PolyConecta (D-86).
- **`RollSpecification`**, **`PtSpecification`**: ficha técnica, bloques Rollo y PT.
- **Almacén de CONTPAQi sincronizado**: código, nombre e id, para ligar ubicaciones (`erp_warehouse_id`).
- **`SalesOrder`**, **`SalesOrderLine`**: pedido libre con moneda y tipo de cambio en el maestro y precio en la línea.
- **`AuthorizationSignature`**: rol firmado, usuario, si actuó como suplente y fecha; una por rol, de usuarios distintos.
- **`ChatterMessage`**: mensaje, nota o registro de estado por documento.
- **`SavedSearch`**: favorito por usuario y lista.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un pedido libre se captura, se confirma y se autoriza con dos firmas de personas distintas en menos de 5 minutos, sin salir de PolyConecta.
- **SC-002**: Ninguna combinación de usuarios y grupos permite que una persona aporte las dos firmas de un pedido; las pruebas de dominio y de API lo cubren, incluido el usuario con los dos roles y el suplente.
- **SC-003**: Toda acción no permitida aparece deshabilitada con su razón y la API la rechaza también si se pide directo; una prueba recorre la matriz de la capa 1 del pedido.
- **SC-004**: La primera sincronización contra el simulador trae el 100 % del catálogo semilla, y una sincronización sin cambios no modifica ningún registro.
- **SC-005**: Con 500 pedidos, la lista responde un filtro, una agrupación o un cambio de página en menos de 1 segundo, y ninguna operación de lista se resuelve en el navegador (una prueba cuenta las consultas).
- **SC-006**: El bridge real atiende lecturas durante una hora con una sola sesión del SDK y sin ventanas de ingreso; el resultado del cotejo de T-06 queda registrado.
- **SC-007**: Todo cambio de estado de un pedido aparece en el chatter y en `StateTransitionLog` con usuario y rol, y el chatter sobrevive a recargar la página.
- **SC-008**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R1 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/`, `scripts/vps/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en la rama `003-pedidos-de-venta` o en ramas que salen de ella, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
2. **Qué se puede tocar.** Solo las carpetas de tu sección (CT-33). Lo común se cambia con los dos líderes. `PolyConecta.Presentation/` es de solo lectura (D-60).
3. **El contrato manda.** Si algo no está en `docs/contratos/bridge-v1.md`, se anota en "Exploración y cambios" y se acuerda entre los dos líderes antes de implementarlo (CT-22).
4. **Ante una duda.** Primero esta spec, luego `docs/diseno/`, luego `docs/contpaq/`. Si nada la resuelve, se anota en "Exploración y cambios" con la pregunta concreta y lo que se hizo mientras tanto.
5. **Dependencias.** Solo las versiones ratificadas (CT-36). Un paquete nuevo se anota en "Exploración y cambios" antes de instalarlo.
6. **Hecho es verificado.** Una tarea está hecha cuando se ejecutaron y pasaron sus pruebas, y se verificó en navegador o en el VPS cuando aplica.
7. **No se debilita una prueba para que pase.**
8. **Escrituras en CONTPAQi.** F1 no escribe en CONTPAQi. Si alguna prueba en el VPS necesita hacerlo, se confirma antes con el usuario (D-130).

---

## Assumptions

- **Solo pedido libre.** El pedido capturado en CONTPAQi no entra por sincronización (D-145); todo pedido nace en PolyConecta y en F2 se da de alta en CONTPAQi (D-113).
- **Moneda por pedido.** Moneda y tipo de cambio van en el maestro y el precio en la línea, como `ALTA_PEDIDO` en el contrato `1.0` (D-146). La moneda se propone con la del cliente y se puede cambiar; el tipo de cambio lo captura AC. Las monedas admitidas son las que el bridge sabe traducir (MXN y USD por omisión).
- **Domicilio de entrega.** El pedido guarda el domicilio de entrega elegido. Llevarlo a CONTPAQi en `ALTA_PEDIDO` y usarlo en la entrega (F6) se resuelve en esas fases; en F1 es dato del pedido (D-149).
- **Editar con firmas revoca.** Un pedido con firmas se puede editar en Borrador o Confirmado, pero guardar el cambio revoca la autorización en automático, con aviso previo y registro en el chatter (D-147).
- **El pedido no tiene planta.** Las reglas de fila por planta (RF-1, RF-2) se construyen como mecanismo y se prueban, pero su primera entidad con planta es la OF (F2). En F1 aplican RF-3 y RF-4 al pedido.
- **Los permisos son configurables por grupo.** El catálogo de permisos (tipo de documento × acción) lo define el código; qué permisos tiene cada grupo es dato y lo edita el Administrador (D-148).
- **Contraseñas.** Las da y las restablece el Administrador; no hay autorregistro ni recuperación por correo en F1.
- **Sincronización.** Cada 15 minutos por omisión. La existencia no se sincroniza: se lee al momento por el bridge (D-87) cuando una pantalla la necesita; en F1 ninguna pantalla del pedido la usa todavía.
- **Las demás pantallas siguen en memoria.** Solo Pedidos y las pantallas nuevas de F1 leen de la API; las otras se conectan en su fase.

---

## Exploración y cambios *(obligatoria, CT-43)*

Las secciones anteriores son el **objetivo primario** de la fase, fijado al ratificar la spec. Todo lo que surja después se registra aquí y se ejecuta en esta misma spec; un cambio menor no abre otra spec. Si un cambio modifica una decisión validada o el contrato del bridge, regístralo también en `docs/diseno/decisiones.md` y anota aquí su número.

| Fecha | Camino | Cambio | Motivo | Impacto (requisitos y tareas) | Decisión |
| :--- | :---: | :--- | :--- | :--- | :---: |
| 2026-10-08 | Común | Alcance del "Diseño sin tarea en el plan": el pedido sincronizado queda fuera de alcance; la revocación, la clasificación propia, la ficha técnica y el cotejo de T-06 entran en F1 | Decisión del usuario al completar la spec | US1, US3, US5; FR-008, FR-017, FR-018, FR-025 | D-145 |
| 2026-10-08 | Común | Moneda y tipo de cambio por pedido, no por línea, alineado con `ALTA_PEDIDO` del contrato `1.0` | D-74 y 04 §3 dicen "por línea"; el contrato firmado lleva una moneda por documento | FR-020, FR-022 | D-146 |
| 2026-10-08 | Común | Propuesta de contrato `1.1`: `clasificacion` opcional en la lectura de productos; `moneda` y `domicilios[]` en la de clientes | La clasificación de CONTPAQi es el valor inicial de D-86, la moneda del cliente propone la del pedido (D-146) y los domicilios definen la entrega (D-149); el contrato no lee ninguno | FR-003, FR-008, FR-017, FR-020 | Pendiente de los dos líderes |
| 2026-10-08 | L2 | Editar un pedido con firmas revoca la autorización en automático, con aviso antes de guardar y registro en el chatter; sustituye "la primera firma bloquea la edición" | Revisión de supuestos con el usuario | US1, FR-024, casos límite | D-147 |
| 2026-10-08 | L2 | Grupos como catálogo con permisos configurables, un grupo por nivel, y asignación de permisos con dos paneles de izquierda a derecha; el componente de dos paneles entra a 07 y a la galería | Revisión de supuestos con el usuario | US2, FR-010, FR-013 | D-148 |
| 2026-10-08 | L2 | "Sincronizar ahora" por catálogo y en general | Revisión de supuestos con el usuario | US3, FR-014 | — |
| 2026-10-08 | Común | La moneda del pedido se propone con la del cliente y se cambia en el maestro; el pedido lleva domicilio de entrega elegido entre los del cliente sincronizados; el precio unitario es columna de la captura de líneas (verificado: `aPrecio` de `tMovimiento` y S-14) | Revisión con el usuario | FR-003, FR-008, FR-020 | D-146, D-149 |
| 2026-10-08 | L2 | En los dos paneles de permisos, los permisos se agrupan en árbol Módulo › Documento o funcionalidad › Acción | Revisión con el usuario | US2, FR-013 | D-148 |
| 2026-10-08 | L2 | Paquete nuevo: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12, solo para credenciales (`IdentityUserContext`, sin roles de Identity) | Regla de autonomía 5; D-32 | FR-009; tareas de 1.2 | research R-01 |
| 2026-10-08 | L2 | P-28 resuelta en el plan: `GET /{modulo}/{lista}/vista` y `POST /{modulo}/{lista}/consulta` con `ConsultaLista` en el cuerpo; la vista de búsqueda de las listas HTTP se declara en el servidor | Primera lista que lee de la API | FR-028, FR-029 | research R-03; pasa a `docs/diseno/` al cerrar |
| 2026-10-08 | L2 | El enlace ubicación ↔ almacén de CONTPAQi se hace en F3, al unificar `StockLocation` y `PolyLocation`; F1 solo sincroniza los almacenes | Las ubicaciones son entidades previas a F0 sin la base común | US3 escenario 8 | data-model §3 |
| 2026-10-08 | L2 | El `Product` de F1 es una entidad nueva en `Domain/Inventario/` (`inv.product`); la previa a F0 queda `[Obsolete]` hasta que su fase rediseñe a quien la usa | No reescribir cinco entidades de otras fases | FR-016 | research R-08 |
| 2026-10-08 | L2 | El flujo en memoria pierde su pedido semilla: el botón "Pedido" de la OF de la réplica queda deshabilitado con "Se conecta en F2"; los guiones de escenario de Pedidos se sustituyen por pruebas extremo a extremo contra la API | Pedidos lee de la API y las otras pantallas siguen en memoria | FR-027 | research R-10 |
| 2026-10-08 | L2 | El agente del pedido es texto libre en F1: el catálogo de agentes de CONTPAQi no está en el contrato | Sin lectura de agentes en `bridge-v1` | FR-020 | data-model §4 |
| 2026-10-08 | Común | Verificado por el usuario en CONTPAQi: `CTIMESTAMP` no es la fecha de última modificación; la moneda que tiene efecto es la del cliente (`CIDMONEDA`); un cliente tiene un domicilio fiscal y N de envío. La sincronización lee completo y compara; `modified_since` no se usa en productos ni clientes | Pendientes de R-06 | US3, US5, FR-001, FR-008, FR-014, FR-020; contrato `1.1` | D-150 |
