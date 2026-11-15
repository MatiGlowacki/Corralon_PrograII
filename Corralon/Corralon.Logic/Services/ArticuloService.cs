using Corralon.Logic.DTOs;
using Corralon.Logic.Exceptions;
using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    public interface IArticuloService : ICrudService<ArticuloDto>
    {
        Task<IEnumerable<ArticuloDto>> GetStockBajo();
        Task<ArticuloDto> AjustarStock(int idArticulo, AjusteStockDto dto, int idUsuario);
        Task<IEnumerable<MovimientoStockDto>> GetMovimientos(int idArticulo);
    }

    public class ArticuloService : CrudService<Articulos, ArticuloDto>, IArticuloService
    {
        public ArticuloService(IUnitOfWork uow) : base(uow, uow.Articulos) { }

        protected override string NombreEntidad => "El artículo";
        protected override string ClavePrimaria => "idArticulo";
        protected override Articulos Nuevo() => new Articulos();
        protected override int ObtenerId(Articulos e) => e.idArticulo;
        protected override void SetActivo(Articulos e, bool activo) => e.activo = activo;

        protected override IQueryable<Articulos> ConIncludes(IQueryable<Articulos> q) =>
            q.Include(e => e.idCategoriaNavigation)
             .Include(e => e.idUnidadMedidaNavigation)
             .Include(e => e.idProveedorNavigation);

        protected override IQueryable<Articulos> Ordenar(IQueryable<Articulos> q) => q.OrderBy(e => e.nombre);

        // El stock NO se aplica acá: cambia solo con ventas y ajustes (queda registrado en MovimientosStock).
        protected override void Aplicar(ArticuloDto dto, Articulos e)
        {
            e.codigo = dto.Codigo.Trim();
            e.nombre = dto.Nombre.Trim();
            e.descripcion = dto.Descripcion?.Trim();
            e.idCategoria = dto.IdCategoria;
            e.idUnidadMedida = dto.IdUnidadMedida;
            e.idProveedor = dto.IdProveedor;
            e.precioUnitario = dto.PrecioUnitario;
            e.stockMinimo = dto.StockMinimo;
            e.activo = dto.Activo;
        }

        protected override ArticuloDto ADto(Articulos e) => new ArticuloDto
        {
            IdArticulo = e.idArticulo,
            Codigo = e.codigo,
            Nombre = e.nombre,
            Descripcion = e.descripcion,
            IdCategoria = e.idCategoria,
            Categoria = e.idCategoriaNavigation?.nombre,
            IdUnidadMedida = e.idUnidadMedida,
            UnidadMedida = e.idUnidadMedidaNavigation?.abreviatura,
            PermiteDecimales = e.idUnidadMedidaNavigation?.permiteDecimales ?? false,
            IdProveedor = e.idProveedor,
            Proveedor = e.idProveedorNavigation?.razonSocial,
            PrecioUnitario = e.precioUnitario,
            StockActual = e.stockActual,
            StockMinimo = e.stockMinimo,
            Activo = e.activo,
            StockBajo = e.stockActual <= e.stockMinimo
        };

        protected override async Task Validar(ArticuloDto dto, int? id)
        {
            var codigo = dto.Codigo.Trim();
            if (await _repo.Query().AnyAsync(x => x.codigo == codigo && (id == null || x.idArticulo != id)))
            {
                throw new BusinessException($"Ya existe un artículo con el código '{codigo}'.");
            }

            var idCat = dto.IdCategoria;
            if (!await _uow.Categorias.Query().AnyAsync(c => c.idCategoria == idCat))
            {
                throw new BusinessException("La categoría indicada no existe.");
            }

            var idUni = dto.IdUnidadMedida;
            var unidad = await _uow.UnidadesMedida.Query().FirstOrDefaultAsync(u => u.idUnidadMedida == idUni);
            if (unidad == null)
            {
                throw new BusinessException("La unidad de medida indicada no existe.");
            }

            if (dto.IdProveedor.HasValue)
            {
                var idProv = dto.IdProveedor.Value;
                if (!await _uow.Proveedores.Query().AnyAsync(p => p.idProveedor == idProv))
                {
                    throw new BusinessException("El proveedor indicado no existe.");
                }
            }

            if (id == null && !unidad.permiteDecimales && dto.StockActual != decimal.Truncate(dto.StockActual))
            {
                throw new BusinessException($"La unidad '{unidad.nombre}' no admite cantidades decimales.");
            }
        }

        // Stock inicial: se carga como una entrada y queda en el historial.
        protected override async Task DespuesDeCrear(Articulos e, ArticuloDto dto, int idUsuario)
        {
            if (dto.StockActual > 0)
            {
                e.stockActual = dto.StockActual;
                await _uow.MovimientosStock.Add(new MovimientosStock
                {
                    idArticulo = e.idArticulo,
                    tipo = "Entrada",
                    motivo = "Ajuste",
                    cantidad = dto.StockActual,
                    idUsuario = idUsuario,
                    observaciones = "Stock inicial"
                });
                await _uow.SaveChanges();
            }
        }

        public async Task<IEnumerable<ArticuloDto>> GetStockBajo()
        {
            var lista = await ConIncludes(_repo.Query())
                .AsNoTracking()
                .Where(a => a.activo && a.stockActual <= a.stockMinimo)
                .OrderBy(a => a.nombre)
                .ToListAsync();
            return lista.Select(ADto).ToList();
        }

        public async Task<ArticuloDto> AjustarStock(int idArticulo, AjusteStockDto dto, int idUsuario)
        {
            await _uow.Transaccion(async () =>
            {
                var articulo = await _repo.Query()
                    .Include(a => a.idUnidadMedidaNavigation)
                    .FirstOrDefaultAsync(a => a.idArticulo == idArticulo);

                if (articulo == null)
                {
                    throw new NotFoundException($"El artículo {idArticulo} no existe.");
                }

                if (!articulo.idUnidadMedidaNavigation.permiteDecimales && dto.Cantidad != decimal.Truncate(dto.Cantidad))
                {
                    throw new BusinessException($"'{articulo.nombre}' se mide en {articulo.idUnidadMedidaNavigation.nombre} y no admite decimales.");
                }

                if (dto.Tipo == "Salida")
                {
                    if (articulo.stockActual < dto.Cantidad)
                    {
                        throw new BusinessException(
                            $"Stock insuficiente de '{articulo.nombre}': hay {articulo.stockActual} y se quieren descontar {dto.Cantidad}.");
                    }
                    articulo.stockActual -= dto.Cantidad;
                }
                else
                {
                    articulo.stockActual += dto.Cantidad;
                }

                await _uow.MovimientosStock.Add(new MovimientosStock
                {
                    idArticulo = articulo.idArticulo,
                    tipo = dto.Tipo,
                    motivo = dto.Motivo,
                    cantidad = dto.Cantidad,
                    idUsuario = idUsuario,
                    observaciones = dto.Observaciones
                });

                await _uow.SaveChanges();
            });

            return await GetById(idArticulo);
        }

        public async Task<IEnumerable<MovimientoStockDto>> GetMovimientos(int idArticulo)
        {
            if (!await _repo.Query().AnyAsync(a => a.idArticulo == idArticulo))
            {
                throw new NotFoundException($"El artículo {idArticulo} no existe.");
            }

            return await _uow.MovimientosStock.Query()
                .AsNoTracking()
                .Where(m => m.idArticulo == idArticulo)
                .OrderByDescending(m => m.fecha)
                .ThenByDescending(m => m.idMovimiento)
                .Take(200)
                .Select(m => new MovimientoStockDto
                {
                    IdMovimiento = m.idMovimiento,
                    IdArticulo = m.idArticulo,
                    Articulo = m.idArticuloNavigation.nombre,
                    Fecha = m.fecha,
                    Tipo = m.tipo,
                    Motivo = m.motivo,
                    Cantidad = m.cantidad,
                    IdFactura = m.idFactura,
                    NroFactura = m.idFacturaNavigation != null ? m.idFacturaNavigation.nroFactura : (int?)null,
                    Usuario = m.idUsuarioNavigation.nombreCompleto,
                    Observaciones = m.observaciones
                })
                .ToListAsync();
        }
    }
}
