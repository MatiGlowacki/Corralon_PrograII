using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Repositories
{
    public class RepartoRepository : GeneralRepository<Repartos>, IRepartoRepository
    {
        public RepartoRepository(CorralonDBContext context) : base(context)
        {
        }

        private IQueryable<Repartos> ConRelaciones()
        {
            return _dbSet
                .Include(r => r.idVehiculoNavigation)
                .Include(r => r.idChoferNavigation)
                .Include(r => r.DetallesReparto).ThenInclude(d => d.idFacturaNavigation).ThenInclude(f => f.idClienteNavigation)
                .Include(r => r.DetallesReparto).ThenInclude(d => d.idLocalidadNavigation);
        }

        public async Task<Repartos?> GetConEntregas(int id, bool seguimiento = false)
        {
            var consulta = ConRelaciones();
            if (!seguimiento)
            {
                consulta = consulta.AsNoTracking();
            }
            return await consulta.FirstOrDefaultAsync(r => r.idReparto == id);
        }

        public async Task<IEnumerable<Repartos>> GetListado(DateOnly? desde, DateOnly? hasta, string? estado)
        {
            var consulta = ConRelaciones().AsNoTracking();

            if (desde.HasValue)
            {
                var d = desde.Value;
                consulta = consulta.Where(r => r.fechaProgramada >= d);
            }
            if (hasta.HasValue)
            {
                var h = hasta.Value;
                consulta = consulta.Where(r => r.fechaProgramada <= h);
            }
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var est = estado;
                consulta = consulta.Where(r => r.estado == est);
            }

            return await consulta
                .OrderByDescending(r => r.fechaProgramada)
                .ThenByDescending(r => r.idReparto)
                .ToListAsync();
        }

        public async Task<bool> VehiculoOcupado(int idVehiculo, DateOnly fecha, int? excluirReparto)
        {
            return await _dbSet.AnyAsync(r =>
                r.idVehiculo == idVehiculo &&
                r.fechaProgramada == fecha &&
                r.estado != "Cancelado" &&
                (excluirReparto == null || r.idReparto != excluirReparto));
        }

        public async Task<bool> ChoferOcupado(int idChofer, DateOnly fecha, int? excluirReparto)
        {
            return await _dbSet.AnyAsync(r =>
                r.idChofer == idChofer &&
                r.fechaProgramada == fecha &&
                r.estado != "Cancelado" &&
                (excluirReparto == null || r.idReparto != excluirReparto));
        }

        public async Task<bool> FacturaEnRepartoVigente(int idFactura, int? excluirReparto)
        {
            return await _context.DetallesReparto.AnyAsync(d =>
                d.idFactura == idFactura &&
                (d.estado == "Pendiente" || d.estado == "Entregada") &&
                d.idRepartoNavigation.estado != "Cancelado" &&
                (excluirReparto == null || d.idReparto != excluirReparto));
        }
    }
}
