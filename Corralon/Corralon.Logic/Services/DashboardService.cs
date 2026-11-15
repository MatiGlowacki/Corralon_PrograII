using Corralon.Logic.DTOs;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> Obtener();
    }

    /// <summary>Información resumida del mes en curso. Los importes se calculan a partir de los detalles de factura.</summary>
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;

        public DashboardService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<DashboardDto> Obtener()
        {
            var ahora = DateTime.Now;
            var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            var inicioDias = ahora.Date.AddDays(-29);

            var facturasMes = _uow.Facturas.Query().AsNoTracking().Where(f => f.fecha >= inicioMes);

            var lineasMes = _uow.DetallesFactura.Query().AsNoTracking()
                .Where(d => d.idFacturaNavigation.estado == "Emitida" && d.idFacturaNavigation.fecha >= inicioMes);

            // Totales del mes
            var emitidas = await facturasMes.CountAsync(f => f.estado == "Emitida");
            var anuladas = await facturasMes.CountAsync(f => f.estado == "Anulada");
            var totalMes = await lineasMes.SumAsync(d => (decimal?)(d.cantidad * d.precioUnitario)) ?? 0m;

            // Ventas por forma de pago
            var totalesPorForma = await lineasMes
                .GroupBy(d => d.idFacturaNavigation.idFormaPago)
                .Select(g => new { IdFormaPago = g.Key, Total = g.Sum(x => x.cantidad * x.precioUnitario) })
                .ToListAsync();

            var cantidadesPorForma = await facturasMes
                .Where(f => f.estado == "Emitida")
                .GroupBy(f => f.idFormaPago)
                .Select(g => new { IdFormaPago = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            var formas = await _uow.FormasPago.GetAll();
            var ventasPorForma = formas
                .Select(fp => new VentaFormaPagoDto
                {
                    FormaPago = fp.nombre,
                    CantidadFacturas = cantidadesPorForma.Where(c => c.IdFormaPago == fp.idFormaPago).Select(c => c.Cantidad).FirstOrDefault(),
                    Total = totalesPorForma.Where(t => t.IdFormaPago == fp.idFormaPago).Select(t => t.Total).FirstOrDefault()
                })
                .Where(v => v.CantidadFacturas > 0)
                .OrderByDescending(v => v.Total)
                .ToList();

            // Artículos más vendidos del mes
            var masVendidos = await lineasMes
                .GroupBy(d => new { d.idArticulo, d.idArticuloNavigation.nombre })
                .OrderByDescending(g => g.Sum(x => x.cantidad))
                .Take(5)
                .Select(g => new ArticuloVendidoDto
                {
                    IdArticulo = g.Key.idArticulo,
                    Articulo = g.Key.nombre,
                    CantidadVendida = g.Sum(x => x.cantidad),
                    Total = g.Sum(x => x.cantidad * x.precioUnitario)
                })
                .ToListAsync();

            // Artículos con stock en el mínimo o por debajo
            var stockBajo = await _uow.Articulos.Query().AsNoTracking()
                .Where(a => a.activo && a.stockActual <= a.stockMinimo)
                .OrderBy(a => a.stockActual - a.stockMinimo)
                .Take(10)
                .Select(a => new ArticuloStockBajoDto
                {
                    IdArticulo = a.idArticulo,
                    Codigo = a.codigo,
                    Articulo = a.nombre,
                    UnidadMedida = a.idUnidadMedidaNavigation.abreviatura,
                    StockActual = a.stockActual,
                    StockMinimo = a.stockMinimo
                })
                .ToListAsync();

            // Ventas de los últimos 30 días (se completan con 0 los días sin ventas)
            var ventasPorDia = await _uow.DetallesFactura.Query().AsNoTracking()
                .Where(d => d.idFacturaNavigation.estado == "Emitida" && d.idFacturaNavigation.fecha >= inicioDias)
                .GroupBy(d => d.idFacturaNavigation.fecha.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.cantidad * x.precioUnitario) })
                .ToListAsync();

            var ultimos30 = Enumerable.Range(0, 30)
                .Select(i => inicioDias.AddDays(i))
                .Select(dia => new VentaDiaDto
                {
                    Fecha = dia,
                    Total = ventasPorDia.Where(v => v.Fecha == dia).Select(v => v.Total).FirstOrDefault()
                })
                .ToList();

            // Repartos pendientes
            var repartosPendientes = await _uow.Repartos.Query().AsNoTracking()
                .CountAsync(r => r.estado == "Programado" || r.estado == "EnCurso");

            var entregasPendientes = await _uow.DetallesReparto.Query().AsNoTracking()
                .CountAsync(d => d.estado == "Pendiente" &&
                                 (d.idRepartoNavigation.estado == "Programado" || d.idRepartoNavigation.estado == "EnCurso"));

            return new DashboardDto
            {
                FacturasEmitidasMes = emitidas,
                FacturasAnuladasMes = anuladas,
                TotalVendidoMes = totalMes,
                TicketPromedio = emitidas > 0 ? Math.Round(totalMes / emitidas, 2) : 0m,
                RepartosPendientes = repartosPendientes,
                EntregasPendientes = entregasPendientes,
                VentasPorFormaPago = ventasPorForma,
                ArticulosMasVendidos = masVendidos,
                ArticulosStockBajo = stockBajo,
                VentasUltimos30Dias = ultimos30
            };
        }
    }
}
