using Corralon.Logic.DTOs;
using Corralon.Logic.Exceptions;
using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    public interface IFacturaService
    {
        Task<IEnumerable<FacturaDto>> GetListado(DateTime? desde, DateTime? hasta, int? idCliente, string? estado);
        Task<FacturaDto> GetById(int id);
        Task<FacturaDto> Crear(FacturaRequestDto dto, int idUsuario);
        Task<FacturaDto> Modificar(int id, FacturaRequestDto dto, int idUsuario);
        Task<FacturaDto> Anular(int id, string motivo, int idUsuario);
    }

    public class FacturaService : IFacturaService
    {
        private const string Emitida = "Emitida";
        private const string Anulada = "Anulada";

        private readonly IUnitOfWork _uow;

        public FacturaService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ------------------------------------------------------------------ Consultas

        public async Task<IEnumerable<FacturaDto>> GetListado(DateTime? desde, DateTime? hasta, int? idCliente, string? estado)
        {
            var facturas = await _uow.Facturas.GetListado(desde, hasta, idCliente, estado);
            return facturas.Select(ADto).ToList();
        }

        public async Task<FacturaDto> GetById(int id)
        {
            var factura = await _uow.Facturas.GetConDetalle(id);
            if (factura == null)
            {
                throw new NotFoundException($"La factura {id} no existe.");
            }
            return ADto(factura);
        }

        // ------------------------------------------------------------------ Alta

        public async Task<FacturaDto> Crear(FacturaRequestDto dto, int idUsuario)
        {
            // Regla del enunciado: si un mismo artículo se agrega más de una vez,
            // se incrementa la cantidad del mismo detalle.
            var lineas = Consolidar(dto.Detalles);

            var idFactura = await _uow.Transaccion(async () =>
            {
                await ValidarCabecera(dto);

                var articulos = await CargarArticulos(lineas.Select(l => l.IdArticulo));

                var factura = new Facturas
                {
                    idCliente = dto.IdCliente,
                    idFormaPago = dto.IdFormaPago,
                    idUsuario = idUsuario,
                    observaciones = dto.Observaciones?.Trim(),
                    estado = Emitida
                };

                foreach (var linea in lineas)
                {
                    var articulo = articulos[linea.IdArticulo];
                    ValidarArticulo(articulo, linea.Cantidad);

                    if (articulo.stockActual < linea.Cantidad)
                    {
                        throw new BusinessException(MensajeStockInsuficiente(articulo, linea.Cantidad));
                    }

                    // El precio queda "congelado" en el detalle al momento de la venta.
                    factura.DetallesFactura.Add(new DetallesFactura
                    {
                        idArticulo = articulo.idArticulo,
                        cantidad = linea.Cantidad,
                        precioUnitario = articulo.precioUnitario
                    });

                    articulo.stockActual -= linea.Cantidad;

                    factura.MovimientosStock.Add(new MovimientosStock
                    {
                        idArticulo = articulo.idArticulo,
                        tipo = "Salida",
                        motivo = "Venta",
                        cantidad = linea.Cantidad,
                        idUsuario = idUsuario,
                        observaciones = "Venta"
                    });
                }

                await _uow.Facturas.Add(factura);
                await _uow.SaveChanges();   // todo o nada: factura + detalles + stock + movimientos
                return factura.idFactura;
            });

            return await GetById(idFactura);
        }

        // ------------------------------------------------------------------ Modificación

        public async Task<FacturaDto> Modificar(int id, FacturaRequestDto dto, int idUsuario)
        {
            var lineas = Consolidar(dto.Detalles);

            await _uow.Transaccion(async () =>
            {
                var factura = await _uow.Facturas.GetConDetalle(id, seguimiento: true);
                if (factura == null)
                {
                    throw new NotFoundException($"La factura {id} no existe.");
                }
                if (factura.estado != Emitida)
                {
                    throw new BusinessException("Solo se pueden modificar facturas emitidas.");
                }
                await ValidarQueNoFueEntregada(id, "modificarse");
                await ValidarCabecera(dto);

                var anteriores = factura.DetallesFactura.ToDictionary(d => d.idArticulo);
                var idsInvolucrados = anteriores.Keys.Union(lineas.Select(l => l.IdArticulo)).ToList();
                var articulos = await CargarArticulos(idsInvolucrados);

                // Renglones nuevos o con cantidad modificada
                foreach (var linea in lineas)
                {
                    var articulo = articulos[linea.IdArticulo];

                    if (anteriores.TryGetValue(linea.IdArticulo, out var detalle))
                    {
                        var diferencia = linea.Cantidad - detalle.cantidad;
                        if (diferencia != 0)
                        {
                            ValidarDecimales(articulo, linea.Cantidad);
                            AplicarDiferenciaDeStock(factura, articulo, diferencia, idUsuario);
                            detalle.cantidad = linea.Cantidad;   // conserva el precio original
                        }
                    }
                    else
                    {
                        ValidarArticulo(articulo, linea.Cantidad);
                        AplicarDiferenciaDeStock(factura, articulo, linea.Cantidad, idUsuario);
                        factura.DetallesFactura.Add(new DetallesFactura
                        {
                            idArticulo = articulo.idArticulo,
                            cantidad = linea.Cantidad,
                            precioUnitario = articulo.precioUnitario
                        });
                    }
                }

                // Renglones que se quitaron: se devuelve el stock
                var idsNuevos = lineas.Select(l => l.IdArticulo).ToHashSet();
                foreach (var detalle in anteriores.Values.Where(d => !idsNuevos.Contains(d.idArticulo)).ToList())
                {
                    var articulo = articulos[detalle.idArticulo];
                    AplicarDiferenciaDeStock(factura, articulo, -detalle.cantidad, idUsuario);
                    _uow.DetallesFactura.Delete(detalle);
                }

                factura.idCliente = dto.IdCliente;
                factura.idFormaPago = dto.IdFormaPago;
                factura.observaciones = dto.Observaciones?.Trim();

                await _uow.SaveChanges();
            });

            return await GetById(id);
        }

        // ------------------------------------------------------------------ Baja (anulación)

        public async Task<FacturaDto> Anular(int id, string motivo, int idUsuario)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw new BusinessException("Hay que indicar el motivo de la anulación.");
            }
            if (motivo.Trim().Length > 200)
            {
                throw new BusinessException("El motivo no puede superar los 200 caracteres.");
            }

            await _uow.Transaccion(async () =>
            {
                var factura = await _uow.Facturas.GetConDetalle(id, seguimiento: true);
                if (factura == null)
                {
                    throw new NotFoundException($"La factura {id} no existe.");
                }
                if (factura.estado == Anulada)
                {
                    throw new BusinessException("La factura ya está anulada.");
                }
                await ValidarQueNoFueEntregada(id, "anularse");

                // Se devuelve el stock de cada renglón
                var articulos = await CargarArticulos(factura.DetallesFactura.Select(d => d.idArticulo));
                foreach (var detalle in factura.DetallesFactura)
                {
                    var articulo = articulos[detalle.idArticulo];
                    articulo.stockActual += detalle.cantidad;

                    factura.MovimientosStock.Add(new MovimientosStock
                    {
                        idArticulo = articulo.idArticulo,
                        tipo = "Entrada",
                        motivo = "Anulacion",
                        cantidad = detalle.cantidad,
                        idUsuario = idUsuario,
                        observaciones = $"Anulación de la factura {factura.nroFactura}"
                    });
                }

                // Las entregas pendientes de esta factura ya no tienen sentido
                var entregasPendientes = await _uow.DetallesReparto.Query()
                    .Where(d => d.idFactura == id && d.estado == "Pendiente")
                    .ToListAsync();
                foreach (var entrega in entregasPendientes)
                {
                    entrega.estado = "NoEntregada";
                    entrega.observaciones = "Factura anulada";
                }

                factura.estado = Anulada;
                factura.fechaAnulacion = DateTime.Now;
                factura.motivoAnulacion = motivo.Trim();

                await _uow.SaveChanges();
            });

            return await GetById(id);
        }

        // ------------------------------------------------------------------ Reglas auxiliares

        private static List<DetalleFacturaItemDto> Consolidar(IEnumerable<DetalleFacturaItemDto> detalles)
        {
            var lista = detalles.ToList();
            if (lista.Count == 0)
            {
                throw new BusinessException("La factura debe tener al menos un artículo.");
            }
            if (lista.Any(d => d.Cantidad <= 0))
            {
                throw new BusinessException("Todas las cantidades deben ser mayores a cero.");
            }

            return lista
                .GroupBy(d => d.IdArticulo)
                .Select(g => new DetalleFacturaItemDto { IdArticulo = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                .ToList();
        }

        private async Task ValidarCabecera(FacturaRequestDto dto)
        {
            var idCliente = dto.IdCliente;
            var cliente = await _uow.Clientes.Query().FirstOrDefaultAsync(c => c.idCliente == idCliente);
            if (cliente == null)
            {
                throw new BusinessException("El cliente indicado no existe.");
            }
            if (!cliente.activo)
            {
                throw new BusinessException($"El cliente '{cliente.nombreCompleto}' está inactivo.");
            }

            var idForma = dto.IdFormaPago;
            var forma = await _uow.FormasPago.Query().FirstOrDefaultAsync(f => f.idFormaPago == idForma);
            if (forma == null)
            {
                throw new BusinessException("La forma de pago indicada no existe.");
            }
            if (!forma.activo)
            {
                throw new BusinessException($"La forma de pago '{forma.nombre}' está inactiva.");
            }
        }

        private async Task<Dictionary<int, Articulos>> CargarArticulos(IEnumerable<int> ids)
        {
            var lista = ids.Distinct().ToList();
            var articulos = await _uow.Articulos.Query()
                .Include(a => a.idUnidadMedidaNavigation)
                .Where(a => lista.Contains(a.idArticulo))
                .ToDictionaryAsync(a => a.idArticulo);

            var faltantes = lista.Where(i => !articulos.ContainsKey(i)).ToList();
            if (faltantes.Count > 0)
            {
                throw new BusinessException($"No existen los artículos: {string.Join(", ", faltantes)}.");
            }
            return articulos;
        }

        private static void ValidarArticulo(Articulos articulo, decimal cantidad)
        {
            if (!articulo.activo)
            {
                throw new BusinessException($"El artículo '{articulo.nombre}' está inactivo.");
            }
            ValidarDecimales(articulo, cantidad);
        }

        private static void ValidarDecimales(Articulos articulo, decimal cantidad)
        {
            var unidad = articulo.idUnidadMedidaNavigation;
            if (!unidad.permiteDecimales && cantidad != decimal.Truncate(cantidad))
            {
                throw new BusinessException(
                    $"'{articulo.nombre}' se vende por {unidad.nombre} y no admite cantidades decimales.");
            }
        }

        private static string MensajeStockInsuficiente(Articulos articulo, decimal pedido)
        {
            return $"Stock insuficiente de '{articulo.nombre}': hay {articulo.stockActual} y se piden {pedido}.";
        }

        /// <summary>diferencia > 0 descuenta stock (más venta); diferencia &lt; 0 lo devuelve.</summary>
        private static void AplicarDiferenciaDeStock(Facturas factura, Articulos articulo, decimal diferencia, int idUsuario)
        {
            if (diferencia > 0)
            {
                if (articulo.stockActual < diferencia)
                {
                    throw new BusinessException(MensajeStockInsuficiente(articulo, diferencia));
                }
                articulo.stockActual -= diferencia;

                factura.MovimientosStock.Add(new MovimientosStock
                {
                    idArticulo = articulo.idArticulo,
                    tipo = "Salida",
                    motivo = "Venta",
                    cantidad = diferencia,
                    idUsuario = idUsuario,
                    observaciones = $"Modificación de la factura {factura.nroFactura}"
                });
            }
            else if (diferencia < 0)
            {
                var devuelto = -diferencia;
                articulo.stockActual += devuelto;

                factura.MovimientosStock.Add(new MovimientosStock
                {
                    idArticulo = articulo.idArticulo,
                    tipo = "Entrada",
                    motivo = "Devolucion",
                    cantidad = devuelto,
                    idUsuario = idUsuario,
                    observaciones = $"Modificación de la factura {factura.nroFactura}"
                });
            }
        }

        private async Task ValidarQueNoFueEntregada(int idFactura, string accion)
        {
            var entregada = await _uow.DetallesReparto.Query()
                .AnyAsync(d => d.idFactura == idFactura && d.estado == "Entregada");
            if (entregada)
            {
                throw new BusinessException($"La factura ya fue entregada y no puede {accion}.");
            }
        }

        private static FacturaDto ADto(Facturas f)
        {
            var detalles = f.DetallesFactura
                .OrderBy(d => d.idDetalle)
                .Select(d => new DetalleFacturaDto
                {
                    IdDetalle = d.idDetalle,
                    IdArticulo = d.idArticulo,
                    Codigo = d.idArticuloNavigation?.codigo ?? string.Empty,
                    Articulo = d.idArticuloNavigation?.nombre ?? string.Empty,
                    UnidadMedida = d.idArticuloNavigation?.idUnidadMedidaNavigation?.abreviatura ?? string.Empty,
                    Cantidad = d.cantidad,
                    PrecioUnitario = d.precioUnitario,
                    Subtotal = Math.Round(d.cantidad * d.precioUnitario, 2)
                })
                .ToList();

            return new FacturaDto
            {
                IdFactura = f.idFactura,
                NroFactura = f.nroFactura,
                Fecha = f.fecha,
                IdCliente = f.idCliente,
                Cliente = f.idClienteNavigation?.nombreCompleto ?? string.Empty,
                IdFormaPago = f.idFormaPago,
                FormaPago = f.idFormaPagoNavigation?.nombre ?? string.Empty,
                IdUsuario = f.idUsuario,
                Vendedor = f.idUsuarioNavigation?.nombreCompleto ?? string.Empty,
                Observaciones = f.observaciones,
                Estado = f.estado,
                FechaAnulacion = f.fechaAnulacion,
                MotivoAnulacion = f.motivoAnulacion,
                Total = detalles.Sum(d => d.Subtotal),
                Detalles = detalles
            };
        }
    }
}
