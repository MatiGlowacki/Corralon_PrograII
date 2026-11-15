using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Repositories
{
    public class FacturaRepository : GeneralRepository<Facturas>, IFacturaRepository
    {
        public FacturaRepository(CorralonDBContext context) : base(context)
        {
        }

        private IQueryable<Facturas> ConRelaciones()
        {
            return _dbSet
                .Include(f => f.idClienteNavigation)
                .Include(f => f.idFormaPagoNavigation)
                .Include(f => f.idUsuarioNavigation)
                .Include(f => f.DetallesFactura)
                    .ThenInclude(d => d.idArticuloNavigation)
                        .ThenInclude(a => a.idUnidadMedidaNavigation);
        }

        public async Task<Facturas?> GetConDetalle(int id, bool seguimiento = false)
        {
            var consulta = ConRelaciones();
            if (!seguimiento)
            {
                consulta = consulta.AsNoTracking();
            }
            return await consulta.FirstOrDefaultAsync(f => f.idFactura == id);
        }

        public async Task<IEnumerable<Facturas>> GetListado(DateTime? desde, DateTime? hasta, int? idCliente, string? estado)
        {
            var consulta = ConRelaciones().AsNoTracking();

            if (desde.HasValue)
            {
                var inicio = desde.Value.Date;
                consulta = consulta.Where(f => f.fecha >= inicio);
            }
            if (hasta.HasValue)
            {
                var finExclusivo = hasta.Value.Date.AddDays(1);
                consulta = consulta.Where(f => f.fecha < finExclusivo);
            }
            if (idCliente.HasValue)
            {
                var cliente = idCliente.Value;
                consulta = consulta.Where(f => f.idCliente == cliente);
            }
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var est = estado;
                consulta = consulta.Where(f => f.estado == est);
            }

            return await consulta
                .OrderByDescending(f => f.fecha)
                .ThenByDescending(f => f.idFactura)
                .ToListAsync();
        }
    }
}
