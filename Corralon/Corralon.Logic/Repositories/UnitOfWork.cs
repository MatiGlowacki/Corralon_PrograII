using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly CorralonDBContext _context;

        public UnitOfWork(CorralonDBContext context)
        {
            _context = context;

            FormasPago = new GeneralRepository<FormasPago>(context);
            Clientes = new GeneralRepository<Clientes>(context);
            Articulos = new GeneralRepository<Articulos>(context);
            Categorias = new GeneralRepository<Categorias>(context);
            Proveedores = new GeneralRepository<Proveedores>(context);
            Vehiculos = new GeneralRepository<Vehiculos>(context);
            Choferes = new GeneralRepository<Choferes>(context);
            Provincias = new GeneralRepository<Provincias>(context);
            Localidades = new GeneralRepository<Localidades>(context);
            CondicionesIva = new GeneralRepository<CondicionesIva>(context);
            UnidadesMedida = new GeneralRepository<UnidadesMedida>(context);
            Roles = new GeneralRepository<Roles>(context);
            Usuarios = new UsuarioRepository(context);

            Facturas = new FacturaRepository(context);
            DetallesFactura = new GeneralRepository<DetallesFactura>(context);
            MovimientosStock = new GeneralRepository<MovimientosStock>(context);
            Repartos = new RepartoRepository(context);
            DetallesReparto = new GeneralRepository<DetallesReparto>(context);
        }

        public IRepository<FormasPago> FormasPago { get; }
        public IRepository<Clientes> Clientes { get; }
        public IRepository<Articulos> Articulos { get; }
        public IRepository<Categorias> Categorias { get; }
        public IRepository<Proveedores> Proveedores { get; }
        public IRepository<Vehiculos> Vehiculos { get; }
        public IRepository<Choferes> Choferes { get; }
        public IRepository<Provincias> Provincias { get; }
        public IRepository<Localidades> Localidades { get; }
        public IRepository<CondicionesIva> CondicionesIva { get; }
        public IRepository<UnidadesMedida> UnidadesMedida { get; }
        public IRepository<Roles> Roles { get; }
        public IUsuarioRepository Usuarios { get; }

        public IFacturaRepository Facturas { get; }
        public IRepository<DetallesFactura> DetallesFactura { get; }
        public IRepository<MovimientosStock> MovimientosStock { get; }
        public IRepartoRepository Repartos { get; }
        public IRepository<DetallesReparto> DetallesReparto { get; }

        public async Task<int> SaveChanges()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<T> Transaccion<T>(Func<Task<T>> trabajo)
        {
            // Si ya hay una transacción abierta (llamadas anidadas), se reutiliza.
            if (_context.Database.CurrentTransaction != null)
            {
                return await trabajo();
            }

            await using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                var resultado = await trabajo();
                await transaccion.CommitAsync();
                return resultado;
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task Transaccion(Func<Task> trabajo)
        {
            await Transaccion<bool>(async () =>
            {
                await trabajo();
                return true;
            });
        }
    }
}
