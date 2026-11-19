// Transacción / Listado de facturas con alta, modificación, detalle y baja/anulación.

const sesion = iniciarPagina({ activo: 'facturas' });

let facturas = [];
let clientes = [];
let formasPago = [];
let articulos = [];
let lineas = [];      // [{ idArticulo, codigo, nombre, unidad, precio, cantidad, permiteDecimales, stock }]
let modo = 'alta';    // alta | editar | ver
let facturaActual = null;

const modalFactura = new bootstrap.Modal(document.getElementById('modal-factura'));

if (sesion) iniciar();

async function iniciar() {
  document.getElementById('btn-nueva').addEventListener('click', () => abrirFactura('alta'));
  document.getElementById('form-filtros').addEventListener('submit', (e) => { e.preventDefault(); cargar(); });
  document.getElementById('btn-limpiar').addEventListener('click', () => {
    document.getElementById('form-filtros').reset();
    cargar();
  });
  document.getElementById('tbody').addEventListener('click', clickEnTabla);
  document.getElementById('btn-agregar').addEventListener('click', agregarLinea);
  document.getElementById('m-cantidad').addEventListener('keydown', (e) => {
    if (e.key === 'Enter') { e.preventDefault(); agregarLinea(); }
  });
  document.getElementById('m-articulo').addEventListener('change', ajustarCantidad);
  document.getElementById('tb-lineas').addEventListener('click', clickEnLineas);
  document.getElementById('form-factura').addEventListener('submit', guardar);
  cargar();
}

//  Listado 

async function cargar() {
  const params = new URLSearchParams();
  const desde = document.getElementById('f-desde').value;
  const hasta = document.getElementById('f-hasta').value;
  const estado = document.getElementById('f-estado').value;
  if (desde) params.set('desde', desde);
  if (hasta) params.set('hasta', hasta);
  if (estado) params.set('estado', estado);

  try {
    facturas = await api('/api/facturas' + (params.toString() ? `?${params}` : ''));
    facturas.sort((a, b) => new Date(b.fecha) - new Date(a.fecha) || b.nroFactura - a.nroFactura);
    dibujar();
  } catch (e) {
    mostrarError(e);
    document.getElementById('tbody').innerHTML = '<tr><td colspan="8" class="vacio">No se pudo cargar el listado.</td></tr>';
  }
}

function dibujar() {
  const cuerpo = document.getElementById('tbody');
  if (!facturas.length) {
    cuerpo.innerHTML = '<tr><td colspan="8" class="vacio">No hay facturas para los filtros elegidos. Usá "Nueva factura" para cargar la primera.</td></tr>';
  } else {
    cuerpo.innerHTML = facturas.map((f) => {
      const emitida = f.estado === 'Emitida';
      let acciones = `<button type="button" class="btn btn-sm btn-outline-secondary" data-accion="ver" data-id="${f.idFactura}">Ver</button>`;
      if (emitida) acciones += ` <button type="button" class="btn btn-sm btn-outline-primary" data-accion="editar" data-id="${f.idFactura}">Modificar</button>`;
      if (emitida && Sesion.esAdmin()) acciones += ` <button type="button" class="btn btn-sm btn-outline-danger" data-accion="anular" data-id="${f.idFactura}">Anular</button>`;
      return `<tr class="${emitida ? '' : 'inactivo'}">
        <td>${fmtNroFactura(f.nroFactura)}</td><td>${fmtFecha(f.fecha)}</td><td>${esc(f.cliente)}</td>
        <td>${esc(f.formaPago)}</td><td>${esc(f.vendedor)}</td>
        <td class="num">${fmtMoneda(f.total)}</td>
        <td><span class="badge ${emitida ? 'text-bg-success' : 'text-bg-secondary'}">${esc(f.estado)}</span></td>
        <td class="text-nowrap">${acciones}</td></tr>`;
    }).join('');
  }
  const emitidas = facturas.filter((f) => f.estado === 'Emitida');
  document.getElementById('resumen').textContent =
    `${facturas.length} factura${facturas.length === 1 ? '' : 's'} en la lista (${emitidas.length} emitida${emitidas.length === 1 ? '' : 's'})`;
  document.getElementById('suma-total').textContent =
    fmtMoneda(emitidas.reduce((acc, f) => acc + f.total, 0));
}

function clickEnTabla(e) {
  const btn = e.target.closest('button[data-accion]');
  if (!btn) return;
  const id = Number(btn.dataset.id);
  if (btn.dataset.accion === 'ver') abrirFactura('ver', id);
  if (btn.dataset.accion === 'editar') abrirFactura('editar', id);
  if (btn.dataset.accion === 'anular') anular(id);
}

//  Formulario (alta / editar / ver) 

async function cargarCatalogos() {
  [clientes, formasPago, articulos] = await Promise.all([
    api('/api/clientes?soloActivos=true'),
    api('/api/formaspago?soloActivos=true'),
    api('/api/articulos?soloActivos=true')
  ]);
}

const opcion = (valor, texto, elegido) =>
  `<option value="${esc(valor)}" ${String(valor) === String(elegido) ? 'selected' : ''}>${esc(texto)}</option>`;

async function abrirFactura(nuevoModo, id = null) {
  modo = nuevoModo;
  facturaActual = null;
  lineas = [];
  const form = document.getElementById('form-factura');
  form.classList.remove('was-validated');

  try {
    await cargarCatalogos();
    if (id) facturaActual = await api(`/api/facturas/${id}`);
  } catch (e) {
    mostrarError(e);
    return;
  }

  const f = facturaActual;
  const soloLectura = modo === 'ver';

  document.getElementById('modal-factura-titulo').textContent =
    modo === 'alta' ? 'Nueva factura'
      : `Factura ${fmtNroFactura(f.nroFactura)}${modo === 'ver' ? '' : ' - modificar'}`;

  // Cliente y forma de pago, si el de la factura ya esta inactivo, igualmente se conserva.
  const listaClientes = [...clientes];
  if (f && !listaClientes.some((c) => c.idCliente === f.idCliente)) listaClientes.push({ idCliente: f.idCliente, nombreCompleto: f.cliente });
  const listaFormas = [...formasPago];
  if (f && !listaFormas.some((x) => x.idFormaPago === f.idFormaPago)) listaFormas.push({ idFormaPago: f.idFormaPago, nombre: f.formaPago });

  document.getElementById('m-cliente').innerHTML =
    '<option value="">Elegí un cliente</option>' +
    listaClientes.map((c) => opcion(c.idCliente, `${c.nombreCompleto} (${c.tipoDocumento ?? ''} ${c.nroDocumento ?? ''})`.replace(' ( )', ''), f?.idCliente)).join('');
  document.getElementById('m-forma').innerHTML =
    '<option value="">Elegí una forma de pago</option>' +
    listaFormas.map((x) => opcion(x.idFormaPago, x.nombre, f?.idFormaPago)).join('');
  document.getElementById('m-obs').value = f?.observaciones ?? '';

  document.getElementById('m-articulo').innerHTML =
    '<option value="">Elegí un artículo</option>' +
    articulos.map((a) => opcion(a.idArticulo, `${a.codigo} - ${a.nombre} (stock ${fmtNumero(a.stockActual)} ${a.unidadMedida})`, '')).join('');
  document.getElementById('m-cantidad').value = '';
  ajustarCantidad();

  if (f) {
    lineas = f.detalles.map((d) => {
      const art = articulos.find((a) => a.idArticulo === d.idArticulo);
      return {
        idArticulo: d.idArticulo, codigo: d.codigo, nombre: d.articulo, unidad: d.unidadMedida,
        precio: d.precioUnitario, cantidad: d.cantidad,
        permiteDecimales: art ? art.permiteDecimales : !Number.isInteger(d.cantidad),
        // Al modificar, el stock disponible incluye lo que esta misma factura ya reservó.
        stock: art ? art.stockActual + (modo === 'editar' ? d.cantidad : 0) : null
      };
    });
  }

  // Datos de anulación (si corresponde)
  const caja = document.getElementById('datos-anulacion');
  if (f && f.estado === 'Anulada') {
    caja.textContent = `Factura anulada el ${fmtFechaHora(f.fechaAnulacion)}. Motivo: ${f.motivoAnulacion ?? '-'}`;
    caja.classList.remove('d-none');
  } else {
    caja.classList.add('d-none');
  }

  // Modo lectura: se bloquea todo y se oculta Guardar.
  ['m-cliente', 'm-forma', 'm-obs'].forEach((i) => (document.getElementById(i).disabled = soloLectura));
  document.getElementById('agregar-linea').classList.toggle('d-none', soloLectura);
  document.getElementById('btn-guardar').classList.toggle('d-none', soloLectura);

  dibujarLineas();
  modalFactura.show();
}

function ajustarCantidad() {
  const art = articulos.find((a) => String(a.idArticulo) === document.getElementById('m-articulo').value);
  const cant = document.getElementById('m-cantidad');
  const decimales = art ? art.permiteDecimales : false;
  cant.step = decimales ? 'any' : '1';
  cant.min = decimales ? '0.001' : '1';
}

function agregarLinea() {
  const art = articulos.find((a) => String(a.idArticulo) === document.getElementById('m-articulo').value);
  const cantidad = Number(document.getElementById('m-cantidad').value);
  if (!art) { toast('Elegí un artículo.', 'warning'); return; }
  if (!(cantidad > 0)) { toast('Ingresá una cantidad mayor a cero.', 'warning'); return; }
  if (!art.permiteDecimales && !Number.isInteger(cantidad)) {
    toast(`"${art.nombre}" se vende por unidad: la cantidad debe ser un número entero.`, 'warning');
    return;
  }

  // Se suma la cantidad si el articulo esta en detalle.
  const existente = lineas.find((l) => l.idArticulo === art.idArticulo);
  const stockDisponible = existente?.stock ?? art.stockActual;
  const totalPedido = (existente ? existente.cantidad : 0) + cantidad;
  if (totalPedido > stockDisponible) {
    toast(`Stock insuficiente de "${art.nombre}": hay ${fmtNumero(stockDisponible)} ${art.unidadMedida}.`, 'warning');
    return;
  }

  if (existente) {
    existente.cantidad = totalPedido;
  } else {
    lineas.push({
      idArticulo: art.idArticulo, codigo: art.codigo, nombre: art.nombre, unidad: art.unidadMedida,
      precio: art.precioUnitario, cantidad, permiteDecimales: art.permiteDecimales,
      stock: art.stockActual
    });
  }
  document.getElementById('m-articulo').value = '';
  document.getElementById('m-cantidad').value = '';
  ajustarCantidad();
  dibujarLineas();
}

function clickEnLineas(e) {
  const btn = e.target.closest('button[data-quitar]');
  if (!btn) return;
  lineas = lineas.filter((l) => String(l.idArticulo) !== btn.dataset.quitar);
  dibujarLineas();
}

function dibujarLineas() {
  const cuerpo = document.getElementById('tb-lineas');
  if (!lineas.length) {
    cuerpo.innerHTML = '<tr><td colspan="6" class="vacio">Todavía no hay artículos. Elegí uno y tocá "Agregar".</td></tr>';
  } else {
    cuerpo.innerHTML = lineas.map((l) => `<tr>
      <td>${esc(l.codigo)}</td><td>${esc(l.nombre)}</td>
      <td class="num">${fmtMoneda(l.precio)}</td>
      <td class="num">${fmtNumero(l.cantidad)} ${esc(l.unidad)}</td>
      <td class="num">${fmtMoneda(l.precio * l.cantidad)}</td>
      <td>${modo === 'ver' ? '' : `<button type="button" class="btn btn-sm btn-outline-danger" data-quitar="${l.idArticulo}" aria-label="Quitar ${esc(l.nombre)}">Quitar</button>`}</td>
    </tr>`).join('');
  }
  const total = lineas.reduce((acc, l) => acc + l.precio * l.cantidad, 0);
  document.getElementById('m-total').textContent = fmtMoneda(total);
}

async function guardar(e) {
  e.preventDefault();
  const form = e.target;
  form.classList.add('was-validated');
  if (!form.checkValidity()) return;
  if (!lineas.length) { toast('La factura debe tener al menos un artículo.', 'warning'); return; }

  const body = {
    idCliente: Number(document.getElementById('m-cliente').value),
    idFormaPago: Number(document.getElementById('m-forma').value),
    observaciones: document.getElementById('m-obs').value.trim() || null,
    detalles: lineas.map((l) => ({ idArticulo: l.idArticulo, cantidad: l.cantidad }))
  };

  const boton = document.getElementById('btn-guardar');
  boton.disabled = true;
  try {
    const resp = modo === 'editar'
      ? await api(`/api/facturas/${facturaActual.idFactura}`, { method: 'PUT', body })
      : await api('/api/facturas', { method: 'POST', body });
    modalFactura.hide();
    toast(modo === 'editar'
      ? `La factura ${fmtNroFactura(resp.nroFactura)} se modificó.`
      : `Se emitió la factura ${fmtNroFactura(resp.nroFactura)} por ${fmtMoneda(resp.total)}.`);
    await cargar();
  } catch (err) {
    mostrarError(err);
  } finally {
    boton.disabled = false;
  }
}

//  Baja/Anulación 

async function anular(id) {
  const f = facturas.find((x) => x.idFactura === id);
  const motivo = await dialogo({
    titulo: `Anular factura ${fmtNroFactura(f.nroFactura)}`,
    mensaje: 'La factura queda anulada y el stock de sus artículos se devuelve.',
    etiqueta: 'Motivo de la anulación', textoOk: 'Anular factura', peligro: true
  });
  if (!motivo) return;
  try {
    await api(`/api/facturas/${id}?motivo=${encodeURIComponent(motivo)}`, { method: 'DELETE' });
    toast(`La factura ${fmtNroFactura(f.nroFactura)} se anuló y el stock se devolvió.`);
    await cargar();
  } catch (e) {
    mostrarError(e);
  }
}
