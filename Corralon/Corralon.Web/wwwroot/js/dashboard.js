// Panel principal

const sesion = iniciarPagina({ activo: 'home' });
if (sesion) {
  document.getElementById('saludo').textContent = `Hola, ${sesion.nombreCompleto}`;
  cargarDashboard();
}

const filaVacia = (cols, texto) => `<tr><td colspan="${cols}" class="vacio">${esc(texto)}</td></tr>`;

async function cargarDashboard() {
  try {
    const d = await api('/api/dashboard');

    document.getElementById('k-emitidas').textContent = fmtNumero(d.facturasEmitidasMes, 0);
    document.getElementById('k-anuladas').textContent = fmtNumero(d.facturasAnuladasMes, 0);
    document.getElementById('k-total').textContent = fmtMoneda(d.totalVendidoMes);
    document.getElementById('k-ticket').textContent = fmtMoneda(d.ticketPromedio);

    dibujarVentasPorDia(d.ventasUltimos30Dias || []);

    document.getElementById('tb-fp').innerHTML = d.ventasPorFormaPago?.length
      ? d.ventasPorFormaPago.map((r) => `<tr><td>${esc(r.formaPago)}</td>
          <td class="num">${fmtNumero(r.cantidadFacturas, 0)}</td><td class="num">${fmtMoneda(r.total)}</td></tr>`).join('')
      : filaVacia(3, 'Todavía no hay ventas este mes.');

    document.getElementById('tb-mv').innerHTML = d.articulosMasVendidos?.length
      ? d.articulosMasVendidos.map((r) => `<tr><td>${esc(r.articulo)}</td>
          <td class="num">${fmtNumero(r.cantidadVendida)}</td><td class="num">${fmtMoneda(r.total)}</td></tr>`).join('')
      : filaVacia(3, 'Todavía no hay ventas este mes.');

    document.getElementById('tb-sb').innerHTML = d.articulosStockBajo?.length
      ? d.articulosStockBajo.map((r) => `<tr><td>${esc(r.codigo)}</td><td>${esc(r.articulo)}</td>
          <td class="num"><span class="badge text-bg-warning">${fmtNumero(r.stockActual)} ${esc(r.unidadMedida)}</span></td>
          <td class="num">${fmtNumero(r.stockMinimo)} ${esc(r.unidadMedida)}</td></tr>`).join('')
      : filaVacia(4, 'Ningún artículo está por debajo de su stock mínimo.');
  } catch (e) {
    const caja = document.getElementById('dashboard-error');
    caja.textContent = e.message;
    caja.classList.remove('d-none');
  }
}

function dibujarVentasPorDia(dias) {
  const cont = document.getElementById('grafico');
  if (!dias.length) {
    cont.innerHTML = '<div class="vacio w-100 align-self-center">No hubo ventas en los últimos 30 días.</div>';
    return;
  }
  const max = Math.max(...dias.map((x) => x.total), 1);
  cont.innerHTML = dias.map((x) => {
    const alto = Math.max((x.total / max) * 100, 1);
    const tip = `${fmtFecha(x.fecha)}: ${fmtMoneda(x.total)}`;
    return `<div class="barra-dia" style="height:${alto}%" title="${esc(tip)}"></div>`;
  }).join('');
  document.getElementById('eje-desde').textContent = fmtFecha(dias[0].fecha);
  document.getElementById('eje-hasta').textContent = fmtFecha(dias[dias.length - 1].fecha);
  document.getElementById('eje-max').textContent = `Máximo diario: ${fmtMoneda(max)}`;
}
