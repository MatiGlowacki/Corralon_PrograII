using Corralon.Logic.DTOs;
using Corralon.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RepartosController : ApiControllerBase
    {
        private readonly IRepartoService _repartos;

        public RepartosController(IRepartoService repartos)
        {
            _repartos = repartos;
        }

        /// <summary>Listado con filtros opcionales: desde, hasta (fechas yyyy-MM-dd) y estado.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RepartoDto>>> GetListado(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] string? estado)
        {
            return Ok(await _repartos.GetListado(desde, hasta, estado));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RepartoDto>> GetById(int id)
        {
            return Ok(await _repartos.GetById(id));
        }

        [HttpPost]
        public async Task<ActionResult<RepartoDto>> Post([FromBody] RepartoRequestDto dto)
        {
            var reparto = await _repartos.Crear(dto, UsuarioId);
            return StatusCode(StatusCodes.Status201Created, reparto);
        }

        /// <summary>Solo repartos en estado Programado.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<RepartoDto>> Put(int id, [FromBody] RepartoRequestDto dto)
        {
            return Ok(await _repartos.Modificar(id, dto));
        }

        /// <summary>Programado → EnCurso (registra la hora de salida).</summary>
        [HttpPost("{id:int}/iniciar")]
        public async Task<ActionResult<RepartoDto>> Iniciar(int id)
        {
            return Ok(await _repartos.Iniciar(id));
        }

        /// <summary>EnCurso → Finalizado (todas las entregas deben tener resultado).</summary>
        [HttpPost("{id:int}/finalizar")]
        public async Task<ActionResult<RepartoDto>> Finalizar(int id)
        {
            return Ok(await _repartos.Finalizar(id));
        }

        /// <summary>Cancela el reparto. Motivo opcional: ?motivo=...</summary>
        [HttpPost("{id:int}/cancelar")]
        public async Task<ActionResult<RepartoDto>> Cancelar(int id, [FromQuery] string? motivo)
        {
            return Ok(await _repartos.Cancelar(id, motivo));
        }

        /// <summary>Registra si una entrega se hizo (Entregada) o no (NoEntregada, con motivo).</summary>
        [HttpPut("{id:int}/entregas/{idDetalleReparto:int}")]
        public async Task<ActionResult<RepartoDto>> RegistrarEntrega(int id, int idDetalleReparto, [FromBody] ResultadoEntregaDto dto)
        {
            return Ok(await _repartos.RegistrarEntrega(id, idDetalleReparto, dto));
        }
    }
}
