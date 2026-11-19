// ABMC de las tablas de soporte. La tabla se elige con ?tabla=<clave>.

//  Listas desplegables (cada una devuelve [{v: valor, t: texto}]) 
const OPCIONES = {
  localidades: async () => (await api('/api/catalogos/localidades'))
    .map((l) => ({ v: l.idLocalidad, t: `${l.nombre} (${l.provincia})` })),
  condicionesIva: async () => (await api('/api/catalogos/condiciones-iva'))
    .map((x) => ({ v: x.id, t: x.nombre })),
  tiposDocumento: async () => ['DNI', 'CUIT', 'CUIL'].map((x) => ({ v: x, t: x })),
  categorias: async () => (await api('/api/categorias?soloActivos=true'))
    .map((x) => ({ v: x.idCategoria, t: x.nombre })),
  unidades: async () => (await api('/api/catalogos/unidades-medida'))
    .map((x) => ({ v: x.idUnidadMedida, t: `${x.nombre} (${x.abreviatura})` })),
  proveedores: async () => (await api('/api/proveedores?soloActivos=true'))
    .map((x) => ({ v: x.idProveedor, t: x.razonSocial }))
};

//  Definición de cada tabla 
// columnas: { t: título, v: fila => valor, num: alinear a la derecha, html: el valor ya es HTML seguro }
// campos:   { n: propiedad del DTO, t: etiqueta, tipo: text|email|number|select, req, max, opciones, texto }
const TABLAS = {
  formaspago: {
    titulo: 'Formas de pago', ruta: '/api/formaspago', id: 'idFormaPago', escribe: 'admin',
    columnas: [{ t: 'Nombre', v: (r) => r.nombre }, { t: 'Descripción', v: (r) => r.descripcion }],
    campos: [
      { n: 'nombre', t: 'Nombre', tipo: 'text', req: true, max: 50 },
      { n: 'descripcion', t: 'Descripción', tipo: 'text', max: 200 }
    ]
  },
  categorias: {
    titulo: 'Categorías', ruta: '/api/categorias', id: 'idCategoria', escribe: 'admin',
    columnas: [{ t: 'Nombre', v: (r) => r.nombre }, { t: 'Descripción', v: (r) => r.descripcion }],
    campos: [
      { n: 'nombre', t: 'Nombre', tipo: 'text', req: true, max: 80 },
      { n: 'descripcion', t: 'Descripción', tipo: 'text', max: 200 }
    ]
  },
  proveedores: {
    titulo: 'Proveedores', ruta: '/api/proveedores', id: 'idProveedor', escribe: 'admin',
    columnas: [
      { t: 'Razón social', v: (r) => r.razonSocial }, { t: 'CUIT', v: (r) => r.cuit },
      { t: 'Teléfono', v: (r) => r.telefono }, { t: 'Email', v: (r) => r.email },
      { t: 'Localidad', v: (r) => r.localidad }
    ],
    campos: [
      { n: 'razonSocial', t: 'Razón social', tipo: 'text', req: true, max: 150 },
      { n: 'cuit', t: 'CUIT', tipo: 'text', req: true, max: 13, ayuda: 'Ejemplo: 30-12345678-9' },
      { n: 'telefono', t: 'Teléfono', tipo: 'text', max: 30 },
      { n: 'email', t: 'Email', tipo: 'email', max: 150 },
      { n: 'domicilio', t: 'Domicilio', tipo: 'text', max: 200 },
      { n: 'idLocalidad', t: 'Localidad', tipo: 'select', opciones: 'localidades', etiquetaFila: 'localidad' }
    ]
  },
  vehiculos: {
    titulo: 'Vehículos', ruta: '/api/vehiculos', id: 'idVehiculo', escribe: 'admin',
    columnas: [
      { t: 'Patente', v: (r) => r.patente }, { t: 'Marca', v: (r) => r.marca },
      { t: 'Modelo', v: (r) => r.modelo }, { t: 'Capacidad (kg)', v: (r) => fmtNumero(r.capacidadKg, 2), num: true }
    ],
    campos: [
      { n: 'patente', t: 'Patente', tipo: 'text', req: true, max: 10 },
      { n: 'marca', t: 'Marca', tipo: 'text', req: true, max: 50 },
      { n: 'modelo', t: 'Modelo', tipo: 'text', max: 50 },
      { n: 'capacidadKg', t: 'Capacidad (kg)', tipo: 'number', req: true, min: 0.01 }
    ]
  },
  choferes: {
    titulo: 'Choferes', ruta: '/api/choferes', id: 'idChofer', escribe: 'admin',
    columnas: [
      { t: 'Nombre completo', v: (r) => r.nombreCompleto }, { t: 'DNI', v: (r) => r.dni },
      { t: 'Teléfono', v: (r) => r.telefono }
    ],
    campos: [
      { n: 'nombreCompleto', t: 'Nombre completo', tipo: 'text', req: true, max: 150 },
      { n: 'dni', t: 'DNI', tipo: 'text', req: true, max: 10 },
      { n: 'telefono', t: 'Teléfono', tipo: 'text', max: 30 }
    ]
  },
  clientes: {
    titulo: 'Clientes', ruta: '/api/clientes', id: 'idCliente', escribe: 'todos',
    columnas: [
      { t: 'Nombre completo', v: (r) => r.nombreCompleto },
      { t: 'Documento', v: (r) => `${r.tipoDocumento} ${r.nroDocumento}` },
      { t: 'Condición IVA', v: (r) => r.condicionIva }, { t: 'Teléfono', v: (r) => r.telefono },
      { t: 'Localidad', v: (r) => r.localidad }
    ],
    campos: [
      { n: 'nombreCompleto', t: 'Nombre completo', tipo: 'text', req: true, max: 150 },
      { n: 'tipoDocumento', t: 'Tipo de documento', tipo: 'select', req: true, opciones: 'tiposDocumento', texto: true },
      { n: 'nroDocumento', t: 'Número de documento', tipo: 'text', req: true, max: 13 },
      { n: 'idCondicionIva', t: 'Condición frente al IVA', tipo: 'select', req: true, opciones: 'condicionesIva', etiquetaFila: 'condicionIva' },
      { n: 'telefono', t: 'Teléfono', tipo: 'text', max: 30 },
      { n: 'email', t: 'Email', tipo: 'email', max: 150 },
      { n: 'domicilio', t: 'Domicilio', tipo: 'text', max: 200 },
      { n: 'idLocalidad', t: 'Localidad', tipo: 'select', opciones: 'localidades', etiquetaFila: 'localidad' }
    ]
  },
  articulos: {
    titulo: 'Artículos y stock', ruta: '/api/articulos', id: 'idArticulo', escribe: 'admin', ajusteStock: true,
    columnas: [
      { t: 'Código', v: (r) => r.codigo }, { t: 'Nombre', v: (r) => r.nombre },
      { t: 'Categoría', v: (r) => r.categoria },
      { t: 'Precio', v: (r) => fmtMoneda(r.precioUnitario), num: true },
      {
        t: 'Stock', num: true, html: true,
        v: (r) => `<span class="${r.stockBajo ? 'badge text-bg-warning' : ''}">${esc(fmtNumero(r.stockActual))} ${esc(r.unidadMedida)}</span>`
      },
      { t: 'Mínimo', v: (r) => fmtNumero(r.stockMinimo), num: true }
    ],
    campos: [
      { col: 'col-md-4', n: 'codigo', t: 'Código', tipo: 'text', req: true, max: 30 },
      { col: 'col-md-8', n: 'nombre', t: 'Nombre', tipo: 'text', req: true, max: 150 },
      { col: 'col-12', n: 'descripcion', t: 'Descripción', tipo: 'text', max: 300 },
      { n: 'idCategoria', t: 'Categoría', tipo: 'select', req: true, opciones: 'categorias', etiquetaFila: 'categoria' },
      { n: 'idUnidadMedida', t: 'Unidad de medida', tipo: 'select', req: true, opciones: 'unidades', etiquetaFila: 'unidadMedida' },
      { col: 'col-12', n: 'idProveedor', t: 'Proveedor', tipo: 'select', opciones: 'proveedores', etiquetaFila: 'proveedor' },
      { col: 'col-md-4', n: 'precioUnitario', t: 'Precio unitario', tipo: 'number', req: true, min: 0 },
      { col: 'col-md-4', n: 'stockActual', t: 'Stock inicial', tipo: 'number', req: true, min: 0, soloAlta: true, ayudaEdicion: 'El stock cambia con las ventas y con "Ajustar stock".' },
      { col: 'col-md-4', n: 'stockMinimo', t: 'Stock mínimo', tipo: 'number', req: true, min: 0 }
    ]
  }
};

// -----------------------------------------------------------------------------------------------------------------

const sesion = iniciarPagina({ activo: new URLSearchParams(location.search).get('tabla') || 'soporte' });
const clave = new URLSearchParams(location.search).get('tabla');
const cfg = TABLAS[clave];

let filas = [];
let editando = null; // fila que se está modificando (null = alta)
const modalForm = new bootstrap.Modal(document.getElementById('modal-form'));
const modalStock = new bootstrap.Modal(document.getElementById('modal-stock'));
let articuloStock = null;

const puedeEscribir = () => cfg.escribe === 'todos' || Sesion.esAdmin();

if (sesion) {
  if (!cfg) {
    document.getElementById('titulo').textContent = 'Tabla de soporte no encontrada';
    document.getElementById('tbody').innerHTML =
      '<tr><td class="vacio">Elegí una opción del menú Soporte.</td></tr>';
  } else {
    iniciar();
  }
}

function iniciar() {
  document.title = `${cfg.titulo} - Corralón`;
  document.getElementById('titulo').textContent = cfg.titulo;

  const btnNuevo = document.getElementById('btn-nuevo');
  if (puedeEscribir()) {
    btnNuevo.classList.remove('d-none');
    btnNuevo.addEventListener('click', () => abrirFormulario(null));
  } else {
    document.getElementById('nota-permisos').textContent =
      'Solo lectura: las altas, modificaciones y bajas las realiza un Administrador.';
  }

  const cabecera = cfg.columnas
    .map((c) => `<th class="${c.num ? 'num' : ''}">${esc(c.t)}</th>`).join('');
  document.getElementById('thead').innerHTML =
    `<tr>${cabecera}<th>Estado</th><th><span class="visually-hidden">Acciones</span></th></tr>`;

  document.getElementById('buscar').addEventListener('input', dibujar);
  document.getElementById('ver-inactivos').addEventListener('change', cargar);
  document.getElementById('form-crud').addEventListener('submit', guardar);
  document.getElementById('form-stock').addEventListener('submit', guardarAjusteStock);
  document.getElementById('tbody').addEventListener('click', clickEnTabla);
  cargar();
}

async function cargar() {
  try {
    const soloActivos = !document.getElementById('ver-inactivos').checked;
    filas = await api(`${cfg.ruta}?soloActivos=${soloActivos}`);
    dibujar();
  } catch (e) {
    mostrarError(e);
    document.getElementById('tbody').innerHTML =
      `<tr><td colspan="${cfg.columnas.length + 2}" class="vacio">No se pudo cargar la lista.</td></tr>`;
  }
}

function dibujar() {
  const texto = document.getElementById('buscar').value.trim().toLowerCase();
  const visibles = filas.filter((r) =>
    !texto || cfg.columnas.some((c) => String(c.v(r) ?? '').toLowerCase().includes(texto)));

  const cuerpo = document.getElementById('tbody');
  if (!visibles.length) {
    cuerpo.innerHTML = `<tr><td colspan="${cfg.columnas.length + 2}" class="vacio">${
      filas.length ? 'Ningún registro coincide con la búsqueda.'
        : puedeEscribir() ? 'Todavía no hay registros. Usá "Nuevo registro" para cargar el primero.' : 'Todavía no hay registros.'}</td></tr>`;
    return;
  }

  cuerpo.innerHTML = visibles.map((r) => {
    const celdas = cfg.columnas.map((c) => {
      const valor = c.v(r);
      return `<td class="${c.num ? 'num' : ''}">${c.html ? valor : esc(valor)}</td>`;
    }).join('');
    const estado = r.activo
      ? '<span class="badge text-bg-success">Activo</span>'
      : '<span class="badge text-bg-secondary">Baja</span>';
    let acciones = '';
    if (puedeEscribir()) {
      acciones = `<button type="button" class="btn btn-sm btn-outline-primary" data-accion="editar" data-id="${r[cfg.id]}">Editar</button>`;
      if (cfg.ajusteStock && Sesion.esAdmin() && r.activo) {
        acciones += ` <button type="button" class="btn btn-sm btn-outline-secondary" data-accion="stock" data-id="${r[cfg.id]}">Ajustar stock</button>`;
      }
      if (r.activo) {
        acciones += ` <button type="button" class="btn btn-sm btn-outline-danger" data-accion="baja" data-id="${r[cfg.id]}">Dar de baja</button>`;
      }
    }
    return `<tr class="${r.activo ? '' : 'inactivo'}">${celdas}<td>${estado}</td><td class="text-nowrap">${acciones}</td></tr>`;
  }).join('');
}

function clickEnTabla(e) {
  const btn = e.target.closest('button[data-accion]');
  if (!btn) return;
  const fila = filas.find((r) => String(r[cfg.id]) === btn.dataset.id);
  if (!fila) return;
  if (btn.dataset.accion === 'editar') abrirFormulario(fila);
  if (btn.dataset.accion === 'baja') darDeBaja(fila);
  if (btn.dataset.accion === 'stock') abrirAjusteStock(fila);
}

//  Alta / modificación 

async function abrirFormulario(fila) {
  editando = fila;
  document.getElementById('modal-form-titulo').textContent =
    fila ? `Modificar - ${cfg.titulo}` : `Nuevo registro - ${cfg.titulo}`;
  const cont = document.getElementById('form-campos');
  cont.innerHTML = '<p class="text-secondary">Cargando...</p>';
  document.getElementById('form-crud').classList.remove('was-validated');
  modalForm.show();

  try {
    // Carga en paralelo las listas desplegables que necesite el formulario.
    const listas = {};
    await Promise.all(cfg.campos.filter((c) => c.tipo === 'select').map(async (c) => {
      listas[c.n] = await OPCIONES[c.opciones]();
    }));

    cont.innerHTML = '<div class="row g-3">' + cfg.campos.map((c) => campoHtml(c, fila, listas[c.n])).join('') + `
      <div class="col-12"><div class="form-check">
        <input class="form-check-input" type="checkbox" id="f-activo" name="activo" ${!fila || fila.activo ? 'checked' : ''}>
        <label class="form-check-label" for="f-activo">Activo</label>
      </div></div></div>`;
    cont.querySelector('input:not([disabled]), select:not([disabled])')?.focus();
  } catch (e) {
    modalForm.hide();
    mostrarError(e);
  }
}

function campoHtml(c, fila, opciones) {
  const id = `f-${c.n}`;
  const valor = fila ? fila[c.n] : null;
  const deshabilitado = fila && c.soloAlta ? 'disabled' : '';
  const req = c.req ? 'required' : '';
  let control;

  if (c.tipo === 'select') {
    let lista = opciones || [];
    // Si el valor actual ya no está en la lista, se conserva igual.
    if (valor != null && !lista.some((o) => String(o.v) === String(valor))) {
      lista = [...lista, { v: valor, t: fila[c.etiquetaFila] ?? String(valor) }];
    }
    control = `<select class="form-select" id="${id}" name="${c.n}" ${req} ${deshabilitado}>
      <option value="">${c.req ? 'Elegí una opción' : 'Sin especificar'}</option>
      ${lista.map((o) => `<option value="${esc(o.v)}" ${String(o.v) === String(valor) ? 'selected' : ''}>${esc(o.t)}</option>`).join('')}
    </select>`;
  } else if (c.tipo === 'number') {
    control = `<input type="number" class="form-control" id="${id}" name="${c.n}" step="any"
      ${c.min != null ? `min="${c.min}"` : ''} value="${esc(valor ?? '')}" ${req} ${deshabilitado}>`;
  } else {
    control = `<input type="${c.tipo}" class="form-control" id="${id}" name="${c.n}"
      ${c.max ? `maxlength="${c.max}"` : ''} value="${esc(valor ?? '')}" ${req} ${deshabilitado}>`;
  }

  const ayuda = fila && c.ayudaEdicion ? c.ayudaEdicion : c.ayuda;
  const col = c.col || (c.max >= 100 ? 'col-12' : 'col-md-6');
  return `<div class="${col}">
    <label class="form-label" for="${id}">${esc(c.t)}${c.req ? ' *' : ''}</label>
    ${control}
    ${ayuda ? `<div class="form-text">${esc(ayuda)}</div>` : ''}
    <div class="invalid-feedback">Revisá este dato.</div>
  </div>`;
}

async function guardar(e) {
  e.preventDefault();
  const form = e.target;
  form.classList.add('was-validated');
  if (!form.checkValidity()) return;

  const body = {};
  for (const c of cfg.campos) {
    const crudo = form.elements[c.n].value;
    if (c.tipo === 'number') body[c.n] = crudo === '' ? null : Number(crudo);
    else if (c.tipo === 'select') body[c.n] = crudo === '' ? null : (c.texto ? crudo : Number(crudo));
    else body[c.n] = crudo.trim() === '' ? null : crudo.trim();
  }
  body.activo = form.elements['activo'].checked;
  if (editando) body[cfg.id] = editando[cfg.id];

  const boton = document.getElementById('btn-guardar');
  boton.disabled = true;
  try {
    if (editando) await api(`${cfg.ruta}/${editando[cfg.id]}`, { method: 'PUT', body });
    else await api(cfg.ruta, { method: 'POST', body });
    modalForm.hide();
    toast(editando ? 'Los cambios se guardaron.' : 'El registro se creó correctamente.');
    await cargar();
  } catch (err) {
    mostrarError(err);
  } finally {
    boton.disabled = false;
  }
}

//  Baja 

async function darDeBaja(fila) {
  const ok = await dialogo({
    titulo: 'Dar de baja',
    mensaje: 'El registro queda inactivo y deja de ofrecerse en las listas. Podés reactivarlo desde "Editar".',
    textoOk: 'Dar de baja', peligro: true
  });
  if (!ok) return;
  try {
    await api(`${cfg.ruta}/${fila[cfg.id]}`, { method: 'DELETE' });
    toast('El registro se dio de baja.');
    await cargar();
  } catch (e) {
    mostrarError(e);
  }
}

//  Ajuste de stock 

function abrirAjusteStock(fila) {
  articuloStock = fila;
  document.getElementById('stock-articulo').textContent =
    `${fila.codigo} - ${fila.nombre} (stock actual: ${fmtNumero(fila.stockActual)} ${fila.unidadMedida})`;
  const form = document.getElementById('form-stock');
  form.reset();
  form.classList.remove('was-validated');
  const cant = document.getElementById('st-cantidad');
  cant.step = fila.permiteDecimales ? 'any' : '1';
  cant.min = fila.permiteDecimales ? '0.001' : '1';
  modalStock.show();
}

async function guardarAjusteStock(e) {
  e.preventDefault();
  e.target.classList.add('was-validated');
  if (!e.target.checkValidity()) return;
  try {
    await api(`${cfg.ruta}/${articuloStock[cfg.id]}/ajustar-stock`, {
      method: 'POST',
      body: {
        tipo: document.getElementById('st-tipo').value,
        motivo: document.getElementById('st-motivo').value,
        cantidad: Number(document.getElementById('st-cantidad').value),
        observaciones: document.getElementById('st-obs').value.trim() || null
      }
    });
    modalStock.hide();
    toast('El ajuste de stock quedó registrado.');
    await cargar();
  } catch (err) {
    mostrarError(err);
  }
}
