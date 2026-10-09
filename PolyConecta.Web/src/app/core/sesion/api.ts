/**
 * Cliente de la API sobre `fetch` (L2-T010). Todas las peticiones de la web pasan por aquí:
 * agrega `X-Requested-With` y `X-Correlation-ID`, manda la cookie y, ante un `401`, deja decidir
 * al manejador de sesión si se repite la petición (diálogo de reanudación) o se va a `/login`.
 *
 * No usa `HttpClient`: `platform-browser` importa `@angular/common/http` y, en cuanto una ruta
 * perezosa lo usa, su código entra a la carga inicial (límite de 89 kB, D-143).
 */

/** Error de la API con el `code` y los errores por campo del `ProblemDetails` (contracts/api-f1.md). */
export class ErrorApi extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
    public readonly errores?: Array<{ campo: string; mensaje: string }>
  ) {
    super(message);
    this.name = 'ErrorApi';
  }
}

/** Decide qué hacer ante un `401`: `true` repite la petición; `false` la deja fallar. */
export type ManejadorNoAutorizado = () => Promise<boolean>;

let manejadorNoAutorizado: ManejadorNoAutorizado | null = null;

/** Lo registra `proveerClienteApi()` desde las rutas perezosas; sin él, un `401` solo falla. */
export function registrarManejadorNoAutorizado(manejador: ManejadorNoAutorizado | null): void {
  manejadorNoAutorizado = manejador;
}

function generarCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'corr-' + Math.random().toString(36).substring(2, 15);
}

/** Hace la petición y devuelve la respuesta tal cual, después de atender un `401`. */
export async function respuestaApi(url: string, opciones?: RequestInit): Promise<Response> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-Requested-With': 'PolyConecta',
    'X-Correlation-ID': generarCorrelationId(),
    ...((opciones?.headers as Record<string, string> | undefined) ?? {}),
  };
  const enviar = () => fetch(url, { ...opciones, headers, credentials: 'same-origin' });

  const res = await enviar();
  // Las peticiones de la propia sesión no se atienden, para no abrir el diálogo en bucle.
  if (res.status !== 401 || url.includes('/plataforma/sesion') || !manejadorNoAutorizado) return res;
  return (await manejadorNoAutorizado()) ? enviar() : res;
}

/** Hace la petición y devuelve el cuerpo JSON; si no es exitosa, lanza `ErrorApi`. */
export async function pedirApi<T>(url: string, opciones?: RequestInit): Promise<T> {
  const res = await respuestaApi(url, opciones);

  if (!res.ok) {
    let mensaje = `Error HTTP ${res.status}`;
    let codigo: string | undefined;
    let errores: Array<{ campo: string; mensaje: string }> | undefined;
    try {
      const err = await res.json();
      codigo = err.code ?? err.title;
      errores = err.errores;
      if (err.detail) mensaje = err.detail;
      else if (err.title) mensaje = err.title;
      else if (err.error) mensaje = err.error;
    } catch {
      // El cuerpo no es JSON: queda el mensaje genérico.
    }
    throw new ErrorApi(mensaje, res.status, codigo, errores);
  }

  if (res.status === 204) {
    return undefined as unknown as T;
  }

  return (await res.json()) as T;
}
