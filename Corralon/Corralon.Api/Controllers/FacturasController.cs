using Corralon.Logic.DTOs;
using Corralon.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FacturasController : ApiControllerBase
    {
        private readonly IFacturaService _facturas;

        public FacturasController(IFacturaService facturas)
        {
            _facturas = facturas;
        }

        /// <summary>Listado con filtros opcionales: desde, hasta (fechas), idCliente y estado (Emitida o Anulada).</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FacturaDto>>> GetListado(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? idCliente,
            [FromQuery] string? estado)
        {
            return Ok(await _facturas.GetListado(desde, hasta, idCliente, estado));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FacturaDto>> GetById(int id)
        {
            return Ok(await _facturas.GetById(id));
        }

        /// <summary>Alta: valida stock, descuenta stock y registra los movimientos en una sola transacción.</summary>
        [HttpPost]
        public async Task<ActionResult<FacturaDto>> Post([FromBody] FacturaRequestDto dto)
        {
            var factura = await _facturas.Crear(dto, UsuarioId);
            return StatusCode(StatusCodes.Status201Created, factura);
        }

        /// <summary>Modificación de una factura emitida (ajusta el stock según la diferencia).</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<FacturaDto>> Put(int id, [FromBody] FacturaRequestDto dto)
        {
            return Ok(await _facturas.Modificar(id, dto, UsuarioId));
        }

        /// <summary>Baja: anula la factura (no se borra) y devuelve el stock. Requiere ?motivo=...</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = NombresRol.Administrador)]
        public async Task<ActionResult<FacturaDto>> Delete(int id, [FromQuery] string motivo)
        {
            return Ok(await _facturas.Anular(id, motivo, UsuarioId));
        }
    }
}
