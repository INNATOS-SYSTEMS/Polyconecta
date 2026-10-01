# Ejecutar la matriz del SDK en el VPS

Guía paso a paso para correr la [matriz de pruebas del SDK](../../docs/contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) en el VPS donde vive CONTPAQi. El detalle técnico del laboratorio está en [README.md](README.md) y las reglas del agente en [AGENTS.md](AGENTS.md).

> **No se clona el repositorio en el VPS.** Al servidor solo viaja un paquete autocontenido: `sdklab.exe`, sus scripts, las plantillas y la matriz. Así no hace falta instalar .NET ni git en el servidor de CONTPAQi, el código completo no queda junto a los datos de producción, y el empaquetador revisa que no viaje ningún secreto. Si aun así necesitas clonar, ve al [anexo](#anexo-compilar-desde-un-clon-en-el-vps).

---

## Antes de empezar

- [ ] **La matriz cubre lo que necesitas.** Hoy no incluye los casos T-07 (remisión ligada al pedido), T-10 (alta de almacenes), T-11 (consumo y remisión con ubicaciones virtuales como almacenes) ni T-12 (alta de pedido con precio unitario y moneda) de [preguntas abiertas](../../docs/diseno/preguntas-abiertas.md). Si la corres así, esas preguntas quedan sin respuesta.
- [ ] **Hay acceso al VPS** por SSH (alias `vps-innatos`) y por escritorio remoto para los pasos manuales en CONTPAQi.
- [ ] **Tienes la credencial de `sa`** de SQL Server. Los scripts la piden y no la guardan.

---

## 1. Preparar la empresa de laboratorio (manual, una vez)

En el VPS, desde CONTPAQi:

1. **Crea la empresa de laboratorio** restaurando un respaldo de producción con el nombre `adPOLYEMPAQUES_LAB`. Usa la herramienta de CONTPAQi para crear o restaurar empresas, no solo SQL Server: la empresa tiene que quedar **registrada** en CONTPAQi.
2. **Verifica que existe un SKU con control de lotes y existencia en al menos dos lotes.** Lo exige la matriz.
3. **Da de alta el almacén WIP** desde la interfaz de CONTPAQi, dentro de la empresa de laboratorio. Corresponde a las pruebas A-03 y A-04; el SDK no tiene función para crear almacenes.

> Nunca hagas estos pasos en la empresa viva `adPOLYEMPAQUES`.

## 2. Empaquetar en tu Mac y copiar al VPS

```bash
cd tools/sdk-lab
./scripts/Build-Package.sh                         # genera dist/sdklab-workstation.zip
scp dist/sdklab-workstation.zip vps-innatos:C:/sdklab/
```

En el VPS, descomprime el zip en `C:\sdklab`.

## 3. Instalar la estación (PowerShell como administrador)

Windows Server no permite ejecutar scripts `.ps1` por defecto; aparece el error *"running scripts is disabled on this system"*. Permítelos solo en esta consola y desbloquea los archivos copiados:

```powershell
cd C:\sdklab
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force   # solo esta ventana; la política del servidor no cambia
Get-ChildItem -Recurse C:\sdklab | Unblock-File                     # quita la marca de "descargado de internet"
```

> Repite `Set-ExecutionPolicy -Scope Process ...` en cada consola nueva donde vayas a correr un script (por ejemplo, `New-Baseline.ps1` en el paso 5).

```powershell
.\scripts\Install-Workstation.ps1 -SqlServer 'localhost\COMPAC01' -LabDatabase 'adPOLYEMPAQUES_LAB' -LiveDatabase 'adPOLYEMPAQUES'
```

El script pide la credencial de `sa` y no la guarda. Con ella:

- Crea el login **`sdklab_ro`**, de solo lectura y limitado a la base de laboratorio.
- Marca la base con la propiedad `POLYCONECTA_LAB=1`. Sin esa marca, `sdklab` se niega a trabajar.
- Guarda la contraseña de `sdklab_ro` en la variable de entorno de máquina `SDKLAB_SQL_PASSWORD`, nunca en un archivo.
- Crea `lab.config.json` a partir de la plantilla.

Después:

1. **Revisa `lab.config.json`**: las rutas reales del SDK (`sdkPath`), de la empresa de laboratorio (`labCompanyPath`) y el nombre de la instancia (`sqlServer`).
2. **Abre una sesión nueva de PowerShell**, para que tome la variable de entorno.

## 3.1 Credenciales de CONTPAQi (sin ellas el SDK se cuelga)

La empresa pide **dos** inicios de sesión (D-108). Si falta alguno, CONTPAQi abre una ventana de ingreso que nadie ve y la llamada espera para siempre:

| Variable de entorno (de máquina) | Uso |
| :--- | :--- |
| `SDKLAB_COMERCIAL_USER` / `SDKLAB_COMERCIAL_PASSWORD` | Usuario de Comercial (hoy `SUPERVISOR`), con `fInicioSesionSDK` antes de `fSetNombrePAQ` |
| `SDKLAB_CONTPAQI_USER` / `SDKLAB_CONTPAQI_PASSWORD` | Usuario centralizado de CONTPAQi (Contabilidad), con `fInicioSesionSDKCONTPAQi` después de `fSetNombrePAQ` |

Una contraseña vacía se deja sin variable. El SDK solo funciona en una **sesión iniciada** de Windows (no como servicio ni por SSH): `sdklab` se lanza con una tarea programada `LogonType Interactive` en la sesión del administrador. Si un proceso se detiene a la fuerza durante el inicio de sesión, cierra también `SDKCONTPAQNG` antes de reintentar. `evidence/progress.log` registra cada llamada al SDK antes y después de ejecutarla.

## 4. Verificar el entorno

```powershell
cd C:\sdklab
.\sdklab.exe env
```

Debe mostrar `process32bit=true`, `sdkDllPresent=true` y `guard.ok=true`.

Esta es la **primera prueba real** del interop: `sdklab` se compiló en el Mac y nunca se ha ejecutado contra el SDK de CONTPAQi. Si falla, detente y revisa el error antes de seguir.

> Si ves `GUARD_REFUSED` (código de salida 3), `sdklab` detectó que la base no es de laboratorio. **No edites la configuración para saltarlo**: revisa que la base termine en `_LAB`, coincida con `labCompanyPath` y tenga la marca `POLYCONECTA_LAB`.

## 5. Tomar la línea base

```powershell
.\scripts\New-Baseline.ps1
```

Genera un respaldo `.bak` limpio de la base de laboratorio en `C:\sdklab\backups\`. **Es obligatorio antes de cualquier prueba que escriba** (bloques B en adelante). `Reset-Lab.ps1` restaura esa línea base; solo se corre con tu autorización.

## 6. Ejecutar la matriz con el agente

El agente sigue [AGENTS.md](AGENTS.md). Dónde corre:

| Opción | Cómo | Cuándo |
| :--- | :--- | :--- |
| **A · Agente en tu Mac** (recomendada) | Ejecuta por SSH: `ssh vps-innatos "C:\sdklab\sdklab.exe <comando>"` | Para empezar: no queda ninguna credencial de IA en el servidor |
| B · Agente en el VPS | Claude Code instalado y autenticado en el VPS, abierto en `C:\sdklab` | Si la latencia por SSH estorba |

**Orden de ejecución:**

1. **F** (solo lectura): puede correr desde el inicio.
2. **A-01 a A-05**: entorno y catálogo.
3. **B-01 a B-05**: traspaso entre almacenes. **Punto de control:** si alguna falla, el agente se detiene y te avisa. De este bloque depende que WIP sea un almacén contable (D-22).
4. **C-02 y C-03** primero, luego el resto de C: varios lotes y cantidades fraccionadas.
5. **D**: devolución.
6. **E**: backorder.
7. **G**: resiliencia del bridge, incluido G-01 (documento huérfano).

**Cómo se prueba cada caso:**

```powershell
.\sdklab.exe snapshot antes-<ID> <SKU...>
.\sdklab.exe run specs\<ID>.json
.\sdklab.exe snapshot despues-<ID> <SKU...>
.\sdklab.exe diff antes-<ID> despues-<ID>
```

Que el SDK responda sin error (`rc=0`) **no es evidencia**: se han visto funciones que devuelven éxito sin guardar nada. El resultado se decide con la diferencia entre las dos fotografías contra el criterio de aceptación de la matriz.

Solo hay dos plantillas escritas (`specs/B-02.traspaso-simple.json` y `specs/C-02.multilote.json`). El agente redacta las demás a partir de la matriz, con códigos reales que obtiene con `sdklab sql`, nunca supuestos. Toda referencia de documento empieza con `LAB`.

## 7. Traer los resultados al repositorio

1. Copia `C:\sdklab\evidence\` completo a tu Mac.
2. Pasa el contenido de `evidence/RESULTADOS.md` a la sección **Registro de resultados** de `docs/contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md`.
3. Actualiza [preguntas abiertas](../../docs/diseno/preguntas-abiertas.md): cada T-NN resuelta pasa a [decisiones.md](../../docs/diseno/decisiones.md). Si un bloque falla, aplica su rama de contingencia del [roadmap](../../docs/ROADMAP.md).
4. Haz commit desde el Mac. La carpeta `evidence/` está en `.gitignore`: guárdala como anexo fuera del repositorio o súbela como adjunto.

---

## Ventana de mantenimiento (tarea A-18)

Pruebas que no se ejecutaron el 1-oct porque reinician el servidor o requieren autorización. El servidor es compartido: avisa antes a quien use el SQL Server de otros proyectos.

1. **Preparar el usuario del bridge (S-04).** Un usuario local de Windows dedicado (por ejemplo `polyconecta-bridge`), con inicio de sesión automático (`Autologon` de Sysinternals, que guarda la contraseña cifrada en LSA). En su sesión, una tarea programada "Al iniciar sesión" con `LogonType Interactive` que levanta `sdklab` (hoy) o el bridge (después). Las variables de la sección 3.1 van como variables de máquina.
2. **Reiniciar el servidor** y, sin conectarse por RDP, comprobar por SSH que el proceso está vivo y que `sdklab probe` responde `opened=true` en la sesión del usuario dedicado. Repetir con un `batch` de 2 pares. Resultado esperado: sin intervención y sin ventanas de ingreso.
3. **Movimiento a los almacenes nuevos (S-09, T-14).** Salida de 2 kg de `MP0010` del almacén 9 y Entrada de 1 kg a `LAB-WIP-PIM` y 1 kg a `LAB-WIP-SC` con el costo de la Salida. Verificar existencias por SQL.
4. **Cerrar el pedido remisionado (S-17).** Sobre el pedido `LAB-S14-PEDIDO` (doc 184348, mov 308118): `sdklab set-mov 184348 308118 CUNIDADESPENDIENTES 0`; si no persiste, probar saldarlo o cancelarlo. Verificar por SQL y en la UI que deja de estar pendiente de surtir.
5. Registrar los resultados en la matriz y quitar las tareas de prueba.

## Aprovechar la misma sesión: .NET 10 (tarea A-10)

`sdklab` está compilado hoy en .NET 8. Para comprobar que el SDK funciona con .NET 10 x86 antes de migrar el bridge (D-67):

1. En el Mac, cambia `TargetFramework` a `net10.0` en `src/SdkLab.csproj` y vuelve a empaquetar.
2. En el VPS, repite `sdklab env` y los bloques **F** y **G**.
3. Si pasan, el bridge puede migrar. Si no, queda en .NET 8 y se registra en [decisiones.md](../../docs/diseno/decisiones.md).

---

## Anexo: compilar desde un clon en el VPS

Solo si no puedes empaquetar en el Mac. Requiere Git y el SDK de .NET 8 o 10 para Windows en el VPS.

```powershell
git clone https://github.com/jponcema/Polyconecta.git C:\src\Polyconecta
cd C:\src\Polyconecta
dotnet publish tools\sdk-lab\src\SdkLab.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o C:\sdklab

# Lo que Build-Package.sh copia (es un script de bash y no corre en PowerShell):
Copy-Item tools\sdk-lab\AGENTS.md, tools\sdk-lab\CLAUDE.md, tools\sdk-lab\lab.config.example.json, tools\sdk-lab\README.md, tools\sdk-lab\EJECUCION_EN_VPS.md C:\sdklab\
New-Item -ItemType Directory -Force C:\sdklab\scripts, C:\sdklab\specs, C:\sdklab\matrix, C:\sdklab\evidence | Out-Null
Copy-Item tools\sdk-lab\scripts\*.ps1 C:\sdklab\scripts\
Copy-Item tools\sdk-lab\specs\*.json C:\sdklab\specs\
Copy-Item docs\contpaq\MATRIZ_PRUEBAS_SDK_WIP_LOTES.md, docs\contpaq\Referencia_SDK_CONTPAQi.md, docs\contpaq\Referencia_BD_CONTPAQi.md C:\sdklab\matrix\
```

A partir de aquí sigue desde el paso 3. Ten en cuenta que el clon trae todo el historial de git, que incluye la contraseña anterior de `sa`: rótala antes (tarea A-2 del roadmap).
