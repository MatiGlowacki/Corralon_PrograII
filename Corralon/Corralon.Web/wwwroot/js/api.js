/* Sesión (token JWT) y cliente de la API. */

const CLAVE_SESION = 'corralon.sesion';

const Sesion = {
  guardar(respuestaLogin) {
    sessionStorage.setItem(CLAVE_SESION, JSON.stringify(respuestaLogin));
  },
  obtener() {
    try {
      const s = JSON.parse(sessionStorage.getItem(CLAVE_SESION));
      if (!s || !s.token) return null;
      if (new Date(s.expira) <= new Date()) { this.cerrar(); return null; }
      return s;
    } catch {
      return null;
    }
  },
  cerrar() { sessionStorage.removeItem(CLAVE_SESION); },
  esAdmin() { return this.obtener()?.rol === 'Administrador'; }
};

class ApiError extends Error {
  constructor(status, message) { super(message); this.status = status; }
}

/* Mensaje de error de la API. */
function mensajeError(status, data) {
  if (data?.detail) return data.detail;
  if (data?.errors) {
    const msgs = Object.values(data.errors).flat();
    if (msgs.length) return msgs.join(' ');
  }
  if (data?.title) return data.title;
  if (status === 403) return 'No tenés permisos para realizar esta acción.';
  if (status === 404) return 'No se encontró el registro pedido.';
  return `Error inesperado (código ${status}).`;
}

/*
  Llama a la API. Agrega el token automáticamente.
  Si la API responde 401 con sesión iniciada, la sesión venció: vuelve al login.
 */
async function api(ruta, { method = 'GET', body, auth = true } = {}) {
  const headers = {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const sesion = Sesion.obtener();
  if (auth && sesion) headers['Authorization'] = 'Bearer ' + sesion.token;

  let resp;
  try {
    resp = await fetch(CONFIG.API_URL + ruta, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined
    });
  } catch {
    throw new ApiError(0, 'No se pudo conectar con la API. Verificá que Corralon.Api esté en ejecución.');
  }

  if (resp.status === 401 && auth) {
    Sesion.cerrar();
    location.href = 'login.html?expirada=1';
    throw new ApiError(401, 'Tu sesión venció. Ingresá nuevamente.');
  }

  if (resp.status === 204) return null;

  const texto = await resp.text();
  let data = null;
  try { data = texto ? JSON.parse(texto) : null; } catch { /* respuesta sin JSON */ }

  if (!resp.ok) throw new ApiError(resp.status, mensajeError(resp.status, data));
  return data;
}
