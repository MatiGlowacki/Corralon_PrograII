using Corralon.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    /// <summary>
    /// ABMC estándar. Leer lo puede hacer cualquier usuario logueado;
    /// escribir (alta, modificación y baja) depende de PuedeEscribir.
    /// </summary>
    [Authorize]
    public abstract class CrudController<TDto> : ApiControllerBase where TDto : class
    {
        protected readonly ICrudService<TDto> _service;

        protected CrudController(ICrudService<TDto> service)
        {
            _service = service;
        }

        /// <summary>Por defecto solo el Administrador modifica las tablas de soporte.</summary>
        protected virtual bool PuedeEscribir => User.IsInRole(NombresRol.Administrador);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TDto>>> GetAll([FromQuery] bool soloActivos = false)
        {
            return Ok(await _service.GetAll(soloActivos));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TDto>> GetById(int id)
        {
            return Ok(await _service.GetById(id));
        }

        [HttpPost]
        public async Task<ActionResult<TDto>> Post([FromBody] TDto dto)
        {
            if (!PuedeEscribir)
            {
                return Forbid();
            }
            var creado = await _service.Create(dto, UsuarioId);
            return StatusCode(StatusCodes.Status201Created, creado);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TDto>> Put(int id, [FromBody] TDto dto)
        {
            if (!PuedeEscribir)
            {
                return Forbid();
            }
            return Ok(await _service.Update(id, dto, UsuarioId));
        }

        /// <summary>Baja lógica: el registro queda inactivo.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!PuedeEscribir)
            {
                return Forbid();
            }
            await _service.Delete(id);
            return NoContent();
        }
    }
}
