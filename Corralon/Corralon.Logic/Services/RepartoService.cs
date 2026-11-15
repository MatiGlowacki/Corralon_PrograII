using Corralon.Logic.DTOs;
using Corralon.Logic.Exceptions;
using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    public interface IRepartoService
    {
        Task<IEnumerable<RepartoDto>> GetListado(DateOnly? desde, DateOnly? hasta, string? estado);
        Task<RepartoDto> GetById(int id);
        Task<RepartoDto> Crear(RepartoRequestDto dto, int idUsuario);
        Task<RepartoDto> Modificar(int id, RepartoRequestDto dto);
        Task<RepartoDto> Iniciar(int id);
        Task<RepartoDto> Finalizar(int id);
        Task<RepartoDto> Cancelar(int id, string? motivo);
        Task<RepartoDto> RegistrarEntrega(int idReparto, int idDetalleReparto, ResultadoEntregaDto dto);
    }

    public class RepartoService : IRepartoService
    {
        private const string Programado = "Programado";
        private const string EnCurso = "EnCurso";
        private const string Finalizado = "Finalizado";
        private const string Cancelado = "Cancelado";

        private readonly IUnitOfWork _uow;

        public RepartoService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ------------------------------------------------------------------ Consultas

        public async Task<IEnumerable<RepartoDto>> GetListado(DateOnly? desde, DateOnly? hasta, string? estado)
        {
            var repartos = await _uow.Repartos.GetListado(desde, hasta, estado);
            return repartos.Select(ADto).ToList();
        }

        public async Task<RepartoDto> GetById(int id)
        {
            var reparto = await _uow.Repartos.GetConEntregas(id);
            if (reparto == null)
            {
                throw new NotFoundException($"El reparto {id} no existe.");
            }
            return ADto(reparto);
        }

        // ------------------------------------------------------------------ Alta

        public async Task<RepartoDto> Crear(RepartoRequestDto dto, int idUsuario)
        {
            var idReparto = await _uow.Transaccion(async () =>
            {
                await ValidarReparto(dto, null);

                var reparto = new Repartos
                {
                    fechaProgramada = dto.FechaProgramada,
                    idVehiculo = dto.IdVehiculo,
                    idChofer = dto.IdChofer,
                    idUsuario = idUsuario,
                    estado = Programado,
                    observaciones = dto.Observaciones?.Trim()
                };
                AgregarEntregas(reparto, dto.Entregas);

                await _uow.Repartos.Add(reparto);
                await _uow.SaveChanges();
                return reparto.idReparto;
            });

            return await GetById(idReparto);
        }

        // ------------------------------------------------------------------ Modificación (solo programados)

        public async Task<RepartoDto> Modificar(int id, RepartoRequestDto dto)
        {
            await _uow.Transaccion(async () =>
            {
                var reparto = await _uow.Repartos.GetConEntregas(id, seguimiento: true);
                if (reparto == null)
                {
                    throw new NotFoundException($"El reparto {id} no existe.");
                }
                if (reparto.estado != Programado)
                {
                    throw new BusinessException("Solo se pueden modificar repartos en estado Programado.");
                }

                await ValidarReparto(dto, id);

                reparto.fechaProgramada = dto.FechaProgramada;
                reparto.idVehiculo = dto.IdVehiculo;
                reparto.idChofer = dto.IdChofer;
                reparto.observaciones = dto.Observaciones?.Trim();

                // Se reemplazan las entregas por las nuevas
                foreach (var entrega in reparto.DetallesReparto.ToList())
                {
                    _uow.DetallesReparto.Delete(entrega);
                }
                await _uow.SaveChanges();

                AgregarEntregas(reparto, dto.Entregas);
                await _uow.SaveChanges();
            });

            return await GetById(id);
        }

        // ------------------------------------------------------------------ Cambios de estado

        public async Task<RepartoDto> Iniciar(int id)
        {
            var reparto = await ObtenerParaCambio(id);
            if (reparto.estado != Programado)
            {
                throw new BusinessException("Solo se puede iniciar un reparto Programado.");
            }
            if (reparto.DetallesReparto.Count == 0)
            {
                throw new BusinessException("El reparto no tiene entregas.");
            }

            reparto.estado = EnCurso;
            reparto.horaSalida = DateTime.Now;
            await _uow.SaveChanges();
            return await GetById(id);
        }

        public async Task<RepartoDto> Finalizar(int id)
        {
            var reparto = await ObtenerParaCambio(id);
            if (reparto.estado != EnCurso)
            {
                throw new BusinessException("Solo se puede finalizar un reparto EnCurso.");
            }
            if (reparto.DetallesReparto.Any(d => d.estado == "Pendiente"))
            {
                throw new BusinessException("Quedan entregas pendientes: registrá si se entregaron o no antes de finalizar.");
            }

            reparto.estado = Finalizado;
            reparto.horaRegreso = DateTime.Now;
            await _uow.SaveChanges();
            return await GetById(id);
        }

        public async Task<RepartoDto> Cancelar(int id, string? motivo)
        {
            var reparto = await ObtenerParaCambio(id);
            if (reparto.estado == Finalizado || reparto.estado == Cancelado)
            {
                throw new BusinessException($"El reparto ya está {reparto.estado}.");
            }
            if (reparto.DetallesReparto.Any(d => d.estado == "Entregada"))
            {
                throw new BusinessException("Ya hay entregas realizadas: no se puede cancelar, hay que finalizarlo.");
            }

            reparto.estado = Cancelado;
            if (!string.IsNullOrWhiteSpace(motivo))
            {
                var texto = $"Cancelado: {motivo.Trim()}";
                reparto.observaciones = string.IsNullOrWhiteSpace(reparto.observaciones)
                    ? texto
                    : $"{reparto.observaciones} | {texto}";
                if (reparto.observaciones.Length > 300)
                {
                    reparto.observaciones = reparto.observaciones.Substring(0, 300);
                }
            }

            await _uow.SaveChanges();
            return await GetById(id);
        }

        public async Task<RepartoDto> RegistrarEntrega(int idReparto, int idDetalleReparto, ResultadoEntregaDto dto)
        {
            var reparto = await ObtenerParaCambio(idReparto);
            if (reparto.estado != EnCurso)
            {
                throw new BusinessException("Solo se registran entregas en un reparto EnCurso.");
            }

            var entrega = reparto.DetallesReparto.FirstOrDefault(d => d.idDetalleReparto == idDetalleReparto);
            if (entrega == null)
            {
                throw new NotFoundException($"La entrega {idDetalleReparto} no pertenece al reparto {idReparto}.");
            }
            if (entrega.estado != "Pendiente")
            {
                throw new BusinessException("Esa entrega ya tiene un resultado registrado.");
            }
            if (dto.Estado == "NoEntregada" && string.IsNullOrWhiteSpace(dto.Observaciones))
            {
                throw new BusinessException("Si no se pudo entregar, indicá el motivo en las observaciones.");
            }

            entrega.estado = dto.Estado;
            entrega.fechaEntrega = dto.Estado == "Entregada" ? DateTime.Now : null;
            entrega.observaciones = dto.Observaciones?.Trim();

            await _uow.SaveChanges();
            return await GetById(idReparto);
        }

        // ------------------------------------------------------------------ Reglas auxiliares

        private async Task<Repartos> ObtenerParaCambio(int id)
        {
            var reparto = await _uow.Repartos.GetConEntregas(id, seguimiento: true);
            if (reparto == null)
            {
                throw new NotFoundException($"El reparto {id} no existe.");
            }
            return reparto;
        }

        private async Task ValidarReparto(RepartoRequestDto dto, int? idRepartoActual)
        {
            if (dto.FechaProgramada < DateOnly.FromDateTime(DateTime.Today))
            {
                throw new BusinessException("La fecha del reparto no puede ser anterior a hoy.");
            }

            var idVehiculo = dto.IdVehiculo;
            var vehiculo = await _uow.Vehiculos.Query().FirstOrDefaultAsync(v => v.idVehiculo == idVehiculo);
            if (vehiculo == null)
            {
                throw new BusinessException("El vehículo indicado no existe.");
            }
            if (!vehiculo.activo)
            {
                throw new BusinessException($"El vehículo {vehiculo.patente} está inactivo.");
            }

            var idChofer = dto.IdChofer;
            var chofer = await _uow.Choferes.Query().FirstOrDefaultAsync(c => c.idChofer == idChofer);
            if (chofer == null)
            {
                throw new BusinessException("El chofer indicado no existe.");
            }
            if (!chofer.activo)
            {
                throw new BusinessException($"El chofer {chofer.nombreCompleto} está inactivo.");
            }

            if (await _uow.Repartos.VehiculoOcupado(dto.IdVehiculo, dto.FechaProgramada, idRepartoActual))
            {
                throw new BusinessException($"El vehículo {vehiculo.patente} ya tiene un reparto ese día.");
            }
            if (await _uow.Repartos.ChoferOcupado(dto.IdChofer, dto.FechaProgramada, idRepartoActual))
            {
                throw new BusinessException($"El chofer {chofer.nombreCompleto} ya tiene un reparto ese día.");
            }

            var idsFactura = dto.Entregas.Select(e => e.IdFactura).ToList();
            if (idsFactura.Distinct().Count() != idsFactura.Count)
            {
                throw new BusinessException("Una misma factura no puede repetirse dentro del reparto.");
            }

            foreach (var entrega in dto.Entregas)
            {
                var idFactura = entrega.IdFactura;
                var factura = await _uow.Facturas.Query().FirstOrDefaultAsync(f => f.idFactura == idFactura);
                if (factura == null)
                {
                    throw new BusinessException($"La factura {idFactura} no existe.");
                }
                if (factura.estado != "Emitida")
                {
                    throw new BusinessException($"La factura {factura.nroFactura} no está emitida.");
                }
                if (await _uow.Repartos.FacturaEnRepartoVigente(idFactura, idRepartoActual))
                {
                    throw new BusinessException($"La factura {factura.nroFactura} ya está asignada a otro reparto.");
                }

                var idLocalidad = entrega.IdLocalidad;
                if (!await _uow.Localidades.Query().AnyAsync(l => l.idLocalidad == idLocalidad))
                {
                    throw new BusinessException($"La localidad {idLocalidad} no existe.");
                }
            }
        }

        private static void AgregarEntregas(Repartos reparto, List<EntregaRequestDto> entregas)
        {
            short orden = 1;
            foreach (var e in entregas)
            {
                reparto.DetallesReparto.Add(new DetallesReparto
                {
                    idFactura = e.IdFactura,
                    ordenEntrega = orden++,
                    domicilioEntrega = e.DomicilioEntrega.Trim(),
                    idLocalidad = e.IdLocalidad,
                    telefonoContacto = e.TelefonoContacto?.Trim(),
                    estado = "Pendiente"
                });
            }
        }

        private static RepartoDto ADto(Repartos r)
        {
            return new RepartoDto
            {
                IdReparto = r.idReparto,
                FechaProgramada = r.fechaProgramada,
                IdVehiculo = r.idVehiculo,
                Vehiculo = r.idVehiculoNavigation == null
                    ? string.Empty
                    : $"{r.idVehiculoNavigation.patente} - {r.idVehiculoNavigation.marca}",
                IdChofer = r.idChofer,
                Chofer = r.idChoferNavigation?.nombreCompleto ?? string.Empty,
                IdUsuario = r.idUsuario,
                Estado = r.estado,
                HoraSalida = r.horaSalida,
                HoraRegreso = r.horaRegreso,
                Observaciones = r.observaciones,
                Entregas = r.DetallesReparto
                    .OrderBy(d => d.ordenEntrega)
                    .Select(d => new EntregaDto
                    {
                        IdDetalleReparto = d.idDetalleReparto,
                        IdFactura = d.idFactura,
                        NroFactura = d.idFacturaNavigation?.nroFactura ?? 0,
                        Cliente = d.idFacturaNavigation?.idClienteNavigation?.nombreCompleto ?? string.Empty,
                        OrdenEntrega = d.ordenEntrega,
                        DomicilioEntrega = d.domicilioEntrega,
                        IdLocalidad = d.idLocalidad,
                        Localidad = d.idLocalidadNavigation?.nombre ?? string.Empty,
                        TelefonoContacto = d.telefonoContacto,
                        Estado = d.estado,
                        FechaEntrega = d.fechaEntrega,
                        Observaciones = d.observaciones
                    })
                    .ToList()
            };
        }
    }
}
