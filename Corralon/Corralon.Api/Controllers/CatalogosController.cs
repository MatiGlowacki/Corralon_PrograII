using Corralon.Logic.DTOs;
using Corralon.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    /// <summary>Listas de solo lectura para completar los combos del front.</summary>
    [ApiController]
    [Route("api/catalogos")]
    [Authorize]
    public class CatalogosController : ApiControllerBase
    {
        private readonly ICatalogoService _catalogos;

        public CatalogosController(ICatalogoService catalogos)
        {
            _catalogos = catalogos;
        }

        [HttpGet("provincias")]
        public async Task<ActionResult<IEnumerable<LookupDto>>> Provincias()
        {
            return Ok(await _catalogos.Provincias());
        }

        [HttpGet("localidades")]
        public async Task<ActionResult<IEnumerable<LocalidadDto>>> Localidades([FromQuery] int? idProvincia)
        {
            return Ok(await _catalogos.Localidades(idProvincia));
        }

        [HttpGet("condiciones-iva")]
        public async Task<ActionResult<IEnumerable<LookupDto>>> CondicionesIva()
        {
            return Ok(await _catalogos.CondicionesIva());
        }

        [HttpGet("unidades-medida")]
        public async Task<ActionResult<IEnumerable<UnidadMedidaDto>>> UnidadesMedida()
        {
            return Ok(await _catalogos.UnidadesMedida());
        }

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<LookupDto>>> Roles()
        {
            return Ok(await _catalogos.Roles());
        }
    }
}
