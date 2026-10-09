import { Page } from '@playwright/test';

/**
 * Vistas de búsqueda tal como las declara la API (`GET …/vista`, contracts/api-listas.md), copiadas de
 * la API real, y favoritos y chatter vacíos: las listas y los formularios HTTP los piden al abrir. Se registra después de las
 * simulaciones de la prueba, para que gane sobre sus rutas más generales.
 */
export const VISTAS: Record<string, unknown> = {
  "plataforma/usuarios": {
    "lista": "plataforma.usuarios",
    "campos": [
      {
        "campo": "usuario",
        "etiqueta": "Usuario"
      },
      {
        "campo": "nombre",
        "etiqueta": "Nombre"
      },
      {
        "campo": "email",
        "etiqueta": "Correo"
      }
    ],
    "filtros": [
      {
        "nombre": "Activos",
        "campo": "Estado"
      },
      {
        "nombre": "Archivados",
        "campo": "Estado"
      }
    ],
    "agrupaciones": [
      {
        "etiqueta": "Estado",
        "campo": "activo"
      }
    ],
    "agrupacionesPorDefecto": []
  },
  "plataforma/grupos": {
    "lista": "plataforma.grupos",
    "campos": [
      {
        "campo": "codigo",
        "etiqueta": "Código"
      },
      {
        "campo": "nombre",
        "etiqueta": "Nombre"
      },
      {
        "campo": "descripcion",
        "etiqueta": "Descripción"
      }
    ],
    "filtros": [
      {
        "nombre": "Activos",
        "campo": "Estado"
      },
      {
        "nombre": "Archivados",
        "campo": "Estado"
      }
    ],
    "agrupaciones": [
      {
        "etiqueta": "Estado",
        "campo": "activo"
      }
    ],
    "agrupacionesPorDefecto": []
  },
  "inventario/productos": {
    "lista": "inventario.productos",
    "campos": [
      {
        "campo": "codigo",
        "etiqueta": "Clave"
      },
      {
        "campo": "nombre",
        "etiqueta": "Nombre"
      }
    ],
    "filtros": [
      {
        "nombre": "Activos",
        "campo": "Estado"
      },
      {
        "nombre": "Archivados",
        "campo": "Estado"
      },
      {
        "nombre": "Con ficha técnica",
        "campo": "Ficha técnica"
      },
      {
        "nombre": "Sin ficha técnica",
        "campo": "Ficha técnica"
      }
    ],
    "agrupaciones": [
      {
        "etiqueta": "Unidad base",
        "campo": "unidadBase"
      },
      {
        "etiqueta": "Clasificación",
        "campo": "clasificacion"
      }
    ],
    "agrupacionesPorDefecto": []
  },
  "ventas/clientes": {
    "lista": "ventas.clientes",
    "campos": [
      {
        "campo": "codigo",
        "etiqueta": "Código"
      },
      {
        "campo": "razonSocial",
        "etiqueta": "Razón social"
      },
      {
        "campo": "rfc",
        "etiqueta": "RFC"
      }
    ],
    "filtros": [
      {
        "nombre": "Activos",
        "campo": "Estado"
      },
      {
        "nombre": "Archivados",
        "campo": "Estado"
      }
    ],
    "agrupaciones": [
      {
        "etiqueta": "Moneda",
        "campo": "moneda"
      }
    ],
    "agrupacionesPorDefecto": []
  },
  "ventas/pedidos": {
    "lista": "ventas.pedidos",
    "campos": [
      {
        "campo": "folio",
        "etiqueta": "Folio"
      },
      {
        "campo": "cliente",
        "etiqueta": "Cliente"
      }
    ],
    "filtros": [
      {
        "nombre": "Borrador",
        "campo": "Estado"
      },
      {
        "nombre": "Confirmado",
        "campo": "Estado"
      },
      {
        "nombre": "Mis pedidos",
        "campo": "Responsable"
      },
      {
        "nombre": "Por autorizar",
        "campo": "Firmas"
      }
    ],
    "agrupaciones": [
      {
        "etiqueta": "Estado",
        "campo": "estado"
      },
      {
        "etiqueta": "Cliente",
        "campo": "cliente"
      }
    ],
    "agrupacionesPorDefecto": []
  }
};

export async function simularListas(page: Page): Promise<void> {
  // El chatter de los documentos guardados pide su historial al abrir el formulario (L2-T032).
  await page.route(/\/api\/v1\/plataforma\/chatter\//, route => route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }));
  await page.route(/\/api\/v1\/plataforma\/favoritos\/[^/]+$/, route =>
    route.request().method() === 'GET' ? route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }) : route.fallback(),
  );
  await page.route(/\/api\/v1\/([a-z]+\/[a-z]+)\/vista$/, route => {
    const lista = /\/api\/v1\/([a-z]+\/[a-z]+)\/vista$/.exec(route.request().url())![1];
    return VISTAS[lista]
      ? route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(VISTAS[lista]) })
      : route.fallback();
  });
}
