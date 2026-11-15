using Corralon.Logic.DTOs;
using Corralon.Logic.Exceptions;
using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    // ---------------------------------------------------------------- Formas de pago
    public class FormaPagoService : CrudService<FormasPago, FormaPagoDto>
    {
        public FormaPagoService(IUnitOfWork uow) : base(uow, uow.FormasPago) { }

        protected override string NombreEntidad => "La forma de pago";
        protected override string ClavePrimaria => "idFormaPago";
        protected override FormasPago Nuevo() => new FormasPago();
        protected override int ObtenerId(FormasPago e) => e.idFormaPago;
        protected override void SetActivo(FormasPago e, bool activo) => e.activo = activo;
        protected override IQueryable<FormasPago> Ordenar(IQueryable<FormasPago> q) => q.OrderBy(e => e.nombre);

        protected override void Aplicar(FormaPagoDto dto, FormasPago e)
        {
            e.nombre = dto.Nombre.Trim();
            e.descripcion = dto.Descripcion?.Trim();
            e.activo = dto.Activo;
        }

        protected override FormaPagoDto ADto(FormasPago e) => new FormaPagoDto
        {
            IdFormaPago = e.idFormaPago,
            Nombre = e.nombre,
            Descripcion = e.descripcion,
            Activo = e.activo
        };

        protected override async Task Validar(FormaPagoDto dto, int? id)
        {
            var nombre = dto.Nombre.Trim();
            var existe = await _repo.Query().AnyAsync(x => x.nombre == nombre && (id == null || x.idFormaPago != id));
            if (existe)
            {
                throw new BusinessException($"Ya existe una forma de pago llamada '{nombre}'.");
            }
        }
    }

    // ---------------------------------------------------------------- Categorías
    public class CategoriaService : CrudService<Categorias, CategoriaDto>
    {
        public CategoriaService(IUnitOfWork uow) : base(uow, uow.Categorias) { }

        protected override string NombreEntidad => "La categoría";
        protected override string ClavePrimaria => "idCategoria";
        protected override Categorias Nuevo() => new Categorias();
        protected override int ObtenerId(Categorias e) => e.idCategoria;
        protected override void SetActivo(Categorias e, bool activo) => e.activo = activo;
        protected override IQueryable<Categorias> Ordenar(IQueryable<Categorias> q) => q.OrderBy(e => e.nombre);

        protected override void Aplicar(CategoriaDto dto, Categorias e)
        {
            e.nombre = dto.Nombre.Trim();
            e.descripcion = dto.Descripcion?.Trim();
            e.activo = dto.Activo;
        }

        protected override CategoriaDto ADto(Categorias e) => new CategoriaDto
        {
            IdCategoria = e.idCategoria,
            Nombre = e.nombre,
            Descripcion = e.descripcion,
            Activo = e.activo
        };

        protected override async Task Validar(CategoriaDto dto, int? id)
        {
            var nombre = dto.Nombre.Trim();
            var existe = await _repo.Query().AnyAsync(x => x.nombre == nombre && (id == null || x.idCategoria != id));
            if (existe)
            {
                throw new BusinessException($"Ya existe una categoría llamada '{nombre}'.");
            }
        }
    }

    // ---------------------------------------------------------------- Proveedores
    public class ProveedorService : CrudService<Proveedores, ProveedorDto>
    {
        public ProveedorService(IUnitOfWork uow) : base(uow, uow.Proveedores) { }

        protected override string NombreEntidad => "El proveedor";
        protected override string ClavePrimaria => "idProveedor";
        protected override Proveedores Nuevo() => new Proveedores();
        protected override int ObtenerId(Proveedores e) => e.idProveedor;
        protected override void SetActivo(Proveedores e, bool activo) => e.activo = activo;
        protected override IQueryable<Proveedores> ConIncludes(IQueryable<Proveedores> q) => q.Include(e => e.idLocalidadNavigation);
        protected override IQueryable<Proveedores> Ordenar(IQueryable<Proveedores> q) => q.OrderBy(e => e.razonSocial);

        protected override void Aplicar(ProveedorDto dto, Proveedores e)
        {
            e.razonSocial = dto.RazonSocial.Trim();
            e.cuit = dto.Cuit.Trim();
            e.telefono = dto.Telefono?.Trim();
            e.email = dto.Email?.Trim();
            e.domicilio = dto.Domicilio?.Trim();
            e.idLocalidad = dto.IdLocalidad;
            e.activo = dto.Activo;
        }

        protected override ProveedorDto ADto(Proveedores e) => new ProveedorDto
        {
            IdProveedor = e.idProveedor,
            RazonSocial = e.razonSocial,
            Cuit = e.cuit,
            Telefono = e.telefono,
            Email = e.email,
            Domicilio = e.domicilio,
            IdLocalidad = e.idLocalidad,
            Localidad = e.idLocalidadNavigation?.nombre,
            Activo = e.activo
        };

        protected override async Task Validar(ProveedorDto dto, int? id)
        {
            var cuit = dto.Cuit.Trim();
            var existe = await _repo.Query().AnyAsync(x => x.cuit == cuit && (id == null || x.idProveedor != id));
            if (existe)
            {
                throw new BusinessException($"Ya existe un proveedor con el CUIT {cuit}.");
            }

            if (dto.IdLocalidad.HasValue)
            {
                var idLoc = dto.IdLocalidad.Value;
                if (!await _uow.Localidades.Query().AnyAsync(l => l.idLocalidad == idLoc))
                {
                    throw new BusinessException("La localidad indicada no existe.");
                }
            }
        }
    }

    // ---------------------------------------------------------------- Vehículos
    public class VehiculoService : CrudService<Vehiculos, VehiculoDto>
    {
        public VehiculoService(IUnitOfWork uow) : base(uow, uow.Vehiculos) { }

        protected override string NombreEntidad => "El vehículo";
        protected override string ClavePrimaria => "idVehiculo";
        protected override Vehiculos Nuevo() => new Vehiculos();
        protected override int ObtenerId(Vehiculos e) => e.idVehiculo;
        protected override void SetActivo(Vehiculos e, bool activo) => e.activo = activo;
        protected override IQueryable<Vehiculos> Ordenar(IQueryable<Vehiculos> q) => q.OrderBy(e => e.patente);

        protected override void Aplicar(VehiculoDto dto, Vehiculos e)
        {
            e.patente = dto.Patente.Trim().ToUpper();
            e.marca = dto.Marca.Trim();
            e.modelo = dto.Modelo?.Trim();
            e.capacidadKg = dto.CapacidadKg;
            e.activo = dto.Activo;
        }

        protected override VehiculoDto ADto(Vehiculos e) => new VehiculoDto
        {
            IdVehiculo = e.idVehiculo,
            Patente = e.patente,
            Marca = e.marca,
            Modelo = e.modelo,
            CapacidadKg = e.capacidadKg,
            Activo = e.activo
        };

        protected override async Task Validar(VehiculoDto dto, int? id)
        {
            var patente = dto.Patente.Trim().ToUpper();
            var existe = await _repo.Query().AnyAsync(x => x.patente == patente && (id == null || x.idVehiculo != id));
            if (existe)
            {
                throw new BusinessException($"Ya existe un vehículo con la patente {patente}.");
            }
        }
    }

    // ---------------------------------------------------------------- Choferes
    public class ChoferService : CrudService<Choferes, ChoferDto>
    {
        public ChoferService(IUnitOfWork uow) : base(uow, uow.Choferes) { }

        protected override string NombreEntidad => "El chofer";
        protected override string ClavePrimaria => "idChofer";
        protected override Choferes Nuevo() => new Choferes();
        protected override int ObtenerId(Choferes e) => e.idChofer;
        protected override void SetActivo(Choferes e, bool activo) => e.activo = activo;
        protected override IQueryable<Choferes> Ordenar(IQueryable<Choferes> q) => q.OrderBy(e => e.nombreCompleto);

        protected override void Aplicar(ChoferDto dto, Choferes e)
        {
            e.nombreCompleto = dto.NombreCompleto.Trim();
            e.dni = dto.Dni.Trim();
            e.telefono = dto.Telefono?.Trim();
            e.activo = dto.Activo;
        }

        protected override ChoferDto ADto(Choferes e) => new ChoferDto
        {
            IdChofer = e.idChofer,
            NombreCompleto = e.nombreCompleto,
            Dni = e.dni,
            Telefono = e.telefono,
            Activo = e.activo
        };

        protected override async Task Validar(ChoferDto dto, int? id)
        {
            var dni = dto.Dni.Trim();
            var existe = await _repo.Query().AnyAsync(x => x.dni == dni && (id == null || x.idChofer != id));
            if (existe)
            {
                throw new BusinessException($"Ya existe un chofer con el DNI {dni}.");
            }
        }
    }

    // ---------------------------------------------------------------- Clientes
    public class ClienteService : CrudService<Clientes, ClienteDto>
    {
        public ClienteService(IUnitOfWork uow) : base(uow, uow.Clientes) { }

        protected override string NombreEntidad => "El cliente";
        protected override string ClavePrimaria => "idCliente";
        protected override Clientes Nuevo() => new Clientes();
        protected override int ObtenerId(Clientes e) => e.idCliente;
        protected override void SetActivo(Clientes e, bool activo) => e.activo = activo;

        protected override IQueryable<Clientes> ConIncludes(IQueryable<Clientes> q) =>
            q.Include(e => e.idCondicionIvaNavigation).Include(e => e.idLocalidadNavigation);

        protected override IQueryable<Clientes> Ordenar(IQueryable<Clientes> q) => q.OrderBy(e => e.nombreCompleto);

        protected override void Aplicar(ClienteDto dto, Clientes e)
        {
            e.nombreCompleto = dto.NombreCompleto.Trim();
            e.tipoDocumento = dto.TipoDocumento;
            e.nroDocumento = dto.NroDocumento.Trim();
            e.idCondicionIva = dto.IdCondicionIva;
            e.telefono = dto.Telefono?.Trim();
            e.email = dto.Email?.Trim();
            e.domicilio = dto.Domicilio?.Trim();
            e.idLocalidad = dto.IdLocalidad;
            e.activo = dto.Activo;
        }

        protected override ClienteDto ADto(Clientes e) => new ClienteDto
        {
            IdCliente = e.idCliente,
            NombreCompleto = e.nombreCompleto,
            TipoDocumento = e.tipoDocumento,
            NroDocumento = e.nroDocumento,
            IdCondicionIva = e.idCondicionIva,
            CondicionIva = e.idCondicionIvaNavigation?.nombre,
            Telefono = e.telefono,
            Email = e.email,
            Domicilio = e.domicilio,
            IdLocalidad = e.idLocalidad,
            Localidad = e.idLocalidadNavigation?.nombre,
            Activo = e.activo
        };

        protected override async Task Validar(ClienteDto dto, int? id)
        {
            var tipo = dto.TipoDocumento;
            var nro = dto.NroDocumento.Trim();
            var existe = await _repo.Query().AnyAsync(x =>
                x.tipoDocumento == tipo && x.nroDocumento == nro && (id == null || x.idCliente != id));
            if (existe)
            {
                throw new BusinessException($"Ya existe un cliente con {tipo} {nro}.");
            }

            var idIva = dto.IdCondicionIva;
            if (!await _uow.CondicionesIva.Query().AnyAsync(c => c.idCondicionIva == idIva))
            {
                throw new BusinessException("La condición de IVA indicada no existe.");
            }

            if (dto.IdLocalidad.HasValue)
            {
                var idLoc = dto.IdLocalidad.Value;
                if (!await _uow.Localidades.Query().AnyAsync(l => l.idLocalidad == idLoc))
                {
                    throw new BusinessException("La localidad indicada no existe.");
                }
            }
        }
    }
}
