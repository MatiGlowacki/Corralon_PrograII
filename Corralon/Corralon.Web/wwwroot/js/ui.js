// Utilidades de interfaz: formatos, avisos y diálogos.

// Escapa texto para insertarlo de forma segura dentro de HTML.
const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) =>
  ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

const fmtMoneda = (n) =>
  new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS' }).format(n ?? 0);
const fmtNumero = (n, dec = 3) =>
  new Intl.NumberFormat('es-AR', { maximumFractionDigits: dec }).format(n ?? 0);
const fmtFecha = (iso) => (iso ? new Date(iso).toLocaleDateString('es-AR') : '');
const fmtFechaHora = (iso) =>
  iso ? new Date(iso).toLocaleString('es-AR', { dateStyle: 'short', timeStyle: 'short' }) : '';
const fmtNroFactura = (n) => String(n).padStart(8, '0');

// Aviso flotante. tipo: success | danger | warning | info
function toast(mensaje, tipo = 'success') {
  let cont = document.getElementById('toasts');
  if (!cont) {
    cont = document.createElement('div');
    cont.id = 'toasts';
    cont.className = 'toast-container position-fixed bottom-0 end-0 p-3';
    document.body.appendChild(cont);
  }
  const el = document.createElement('div');
  const textoOscuro = tipo === 'warning' ? 'text-bg-warning' : `text-bg-${tipo}`;
  el.className = `toast align-items-center ${textoOscuro} border-0`;
  el.setAttribute('role', tipo === 'danger' ? 'alert' : 'status');
  el.innerHTML = `<div class="d-flex"><div class="toast-body"></div>
    <button type="button" class="btn-close ${tipo === 'warning' ? '' : 'btn-close-white'} me-2 m-auto"
            data-bs-dismiss="toast" aria-label="Cerrar"></button></div>`;
  el.querySelector('.toast-body').textContent = mensaje;
  cont.appendChild(el);
  el.addEventListener('hidden.bs.toast', () => el.remove());
  new bootstrap.Toast(el, { delay: tipo === 'danger' ? 7000 : 3500 }).show();
}

const mostrarError = (e) => toast(e?.message || 'Ocurrió un error inesperado.', 'danger');

/*
  Diálogo de confirmación, con campo de texto opcional.
  Devuelve: true (si confirmo), el texto ingresado (si hay etiqueta) o null (si cancelo).
 */
function dialogo({ titulo, mensaje = '', etiqueta = null, textoOk = 'Aceptar', peligro = false }) {
  return new Promise((resolve) => {
    const div = document.createElement('div');
    div.className = 'modal fade';
    div.tabIndex = -1;
    div.innerHTML = `
      <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
        <div class="modal-header"><h2 class="modal-title"></h2>
          <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cerrar"></button></div>
        <div class="modal-body">
          <p class="mb-2 dlg-msg"></p>
          <div class="dlg-campo d-none"><label class="form-label dlg-etiqueta"></label>
            <input type="text" class="form-control" maxlength="200">
            <div class="invalid-feedback">Este dato es obligatorio.</div></div>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancelar</button>
          <button type="button" class="btn dlg-ok"></button>
        </div>
      </div></div>`;
    document.body.appendChild(div);

    div.querySelector('.modal-title').textContent = titulo;
    div.querySelector('.dlg-msg').textContent = mensaje;
    const ok = div.querySelector('.dlg-ok');
    ok.textContent = textoOk;
    ok.classList.add(peligro ? 'btn-danger' : 'btn-primary');
    const input = div.querySelector('input');
    if (etiqueta) {
      div.querySelector('.dlg-campo').classList.remove('d-none');
      div.querySelector('.dlg-etiqueta').textContent = etiqueta;
    }

    let resultado = null;
    const modal = new bootstrap.Modal(div);
    ok.addEventListener('click', () => {
      if (etiqueta && !input.value.trim()) { input.classList.add('is-invalid'); input.focus(); return; }
      resultado = etiqueta ? input.value.trim() : true;
      modal.hide();
    });
    div.addEventListener('shown.bs.modal', () => (etiqueta ? input : ok).focus());
    div.addEventListener('hidden.bs.modal', () => { div.remove(); resolve(resultado); });
    modal.show();
  });
}
