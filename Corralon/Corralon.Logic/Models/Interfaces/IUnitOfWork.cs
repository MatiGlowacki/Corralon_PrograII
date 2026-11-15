namespace Corralon.Logic.Models.Interfaces
{
    /// <summary>
    /// Agrupa los repositorios que comparten un mismo DbContext y coordina
    /// el guardado y las transacciones (todo o nada).
    /// </summary>
    public interface IUnitOfWork
    {
        // Soporte y datos maestros
        IRepository<FormasPago> FormasPago { get; }
        IRepository<Clientes> Clientes { get; }
        IRepository<Articulos> Articulos { get; }
        IRepository<Categorias> Categorias { get; }
        IRepository<Proveedores> Proveedores { get; }
        IRepository<Vehiculos> Vehiculos { get; }
        IRepository<Choferes> Choferes { get; }
        IRepository<Provincias> Provincias { get; }
        IRepository<Localidades> Localidades { get; }
        IRepository<CondicionesIva> CondicionesIva { get; }
        IRepository<UnidadesMedida> UnidadesMedida { get; }
        IRepository<Roles> Roles { get; }
        IUsuarioRepository Usuarios { get; }

        // Transacciones
        IFacturaRepository Facturas { get; }
        IRepository<DetallesFactura> DetallesFactura { get; }
        IRepository<MovimientosStock> MovimientosStock { get; }
        IRepartoRepository Repartos { get; }
        IRepository<DetallesReparto> DetallesReparto { get; }

        /// <summary>Confirma en la base todos los cambios pendientes.</summary>
        Task<int> SaveChanges();

        /// <summary>Ejecuta el trabajo dentro de una transacción: si algo falla, se revierte todo.</summary>
        Task<T> Transaccion<T>(Func<Task<T>> trabajo);

        Task Transaccion(Func<Task> trabajo);
    }
}
