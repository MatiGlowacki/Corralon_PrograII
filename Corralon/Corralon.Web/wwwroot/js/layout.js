// Barra de navegación y control de acceso. Cada página llama a iniciarPagina().

const MENU = [
  { id: 'home', texto: 'Inicio', href: 'home.html' },
  {
    id: 'soporte', texto: 'Soporte', items: [
      { id: 'formaspago', t: 'Formas de pago', href: 'soporte.html?tabla=formaspago' },
      { id: 'categorias', t: 'Categorías', href: 'soporte.html?tabla=categorias' },
      { id: 'proveedores', t: 'Proveedores', href: 'soporte.html?tabla=proveedores' },
      { id: 'vehiculos', t: 'Vehículos', href: 'soporte.html?tabla=vehiculos' },
      { id: 'choferes', t: 'Choferes', href: 'soporte.html?tabla=choferes' },
      { id: 'clientes', t: 'Clientes', href: 'soporte.html?tabla=clientes' },
      { id: 'articulos', t: 'Artículos y stock', href: 'soporte.html?tabla=articulos' }
    ]
  },
  {
    id: 'transaccion', texto: 'Transacción', items: [
      { id: 'facturas', t: 'Facturas', href: 'facturas.html' }
    ]
  },
  {
    id: 'acerca', texto: 'Acerca de', items: [
      { id: 'acerca', t: 'Responsables del desarrollo', href: 'acerca.html' }
    ]
  }
];

function construirMenu(activo) {
  return MENU.map((m) => {
    if (!m.items) {
      return `<li class="nav-item"><a class="nav-link ${m.id === activo ? 'activo' : ''}" href="${m.href}">${esc(m.texto)}</a></li>`;
    }
    const grupoActivo = m.id === activo || m.items.some((i) => i.id === activo);
    const items = m.items.map((i) =>
      `<li><a class="dropdown-item ${i.id === activo ? 'activo' : ''}" href="${i.href}">${esc(i.t)}</a></li>`).join('');
    return `<li class="nav-item dropdown">
      <a class="nav-link dropdown-toggle ${grupoActivo ? 'activo' : ''}" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">${esc(m.texto)}</a>
      <ul class="dropdown-menu">${items}</ul></li>`;
  }).join('');
}

function construirNav(sesion, activo) {
  const derecha = sesion
    ? `<span class="usuario me-3">${esc(sesion.nombreCompleto)} (${esc(sesion.rol)})</span>
       <button type="button" class="btn btn-sm btn-outline-light" id="btn-salir">Salir</button>`
    : `<a class="btn btn-sm btn-senal" href="login.html">Ingresar</a>`;
  return `<nav class="navbar navbar-expand-lg barra">
    <div class="container">
      <a class="navbar-brand" href="${sesion ? 'home.html' : 'index.html'}"><span class="marca"></span>Corralón</a>
      <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#menu-principal"
              aria-controls="menu-principal" aria-expanded="false" aria-label="Mostrar menú"><span class="navbar-toggler-icon"></span></button>
      <div class="collapse navbar-collapse" id="menu-principal">
        <ul class="navbar-nav me-auto">${sesion ? construirMenu(activo) : ''}</ul>
        <div class="d-flex align-items-center py-2 py-lg-0">${derecha}</div>
      </div>
    </div></nav>`;
}

/*
  Inicializa una página.
   - requiereLogin: si no hay sesión válida, redirige al login.
   - activo: id del ítem de menú a resaltar.
  Devuelve la sesión (o null en páginas públicas sin sesión).
 */
function iniciarPagina({ requiereLogin = true, activo = '' } = {}) {
  const sesion = Sesion.obtener();
  if (requiereLogin && !sesion) {
    location.replace('login.html');
    return null;
  }
  const nav = document.getElementById('nav');
  if (nav) nav.innerHTML = construirNav(sesion, activo);
  document.getElementById('btn-salir')?.addEventListener('click', () => {
    Sesion.cerrar();
    location.href = 'index.html';
  });
  return sesion;
}
