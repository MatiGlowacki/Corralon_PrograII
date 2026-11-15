namespace Corralon.Logic.Models.Interfaces
{
    public interface IFacturaRepository : IRepository<Facturas>
    {
        /// <summary>Factura con cliente, forma de pago, vendedor y detalles (con artículo y unidad).</summary>
        Task<Facturas?> GetConDetalle(int id, bool seguimiento = false);

        Task<IEnumerable<Facturas>> GetListado(DateTime? desde, DateTime? hasta, int? idCliente, string? estado);
    }

    public interface IRepartoRepository : IRepository<Repartos>
    {
        /// <summary>Reparto con vehículo, chofer y entregas (con factura, cliente y localidad).</summary>
        Task<Repartos?> GetConEntregas(int id, bool seguimiento = false);

        Task<IEnumerable<Repartos>> GetListado(DateOnly? desde, DateOnly? hasta, string? estado);

        /// <summary>¿El vehículo ya tiene un reparto (no cancelado) ese día?</summary>
        Task<bool> VehiculoOcupado(int idVehiculo, DateOnly fecha, int? excluirReparto);

        /// <summary>¿El chofer ya tiene un reparto (no cancelado) ese día?</summary>
        Task<bool> ChoferOcupado(int idChofer, DateOnly fecha, int? excluirReparto);

        /// <summary>¿La factura ya está en un reparto vigente (entrega pendiente o entregada)?</summary>
        Task<bool> FacturaEnRepartoVigente(int idFactura, int? excluirReparto);
    }

    public interface IUsuarioRepository : IRepository<Usuarios>
    {
        /// <summary>Usuario (con su rol) por nombre de usuario, con seguimiento de cambios.</summary>
        Task<Usuarios?> GetByNombreUsuario(string nombreUsuario);
    }
}
