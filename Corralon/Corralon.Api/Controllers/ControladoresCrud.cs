using Corralon.Logic.DTOs;
using Corralon.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    // Soporte: solo el Administrador da de alta, modifica o da de baja.

    [ApiController]
    [Route("api/[controller]")]
    public class FormasPagoController : CrudController<FormaPagoDto>
    {
        public FormasPagoController(ICrudService<FormaPagoDto> service) : base(service) { }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : CrudController<CategoriaDto>
    {
        public CategoriasController(ICrudService<CategoriaDto> service) : base(service) { }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : CrudController<ProveedorDto>
    {
        public ProveedoresController(ICrudService<ProveedorDto> service) : base(service) { }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class VehiculosController : CrudController<VehiculoDto>
    {
        public VehiculosController(ICrudService<VehiculoDto> service) : base(service) { }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ChoferesController : CrudController<ChoferDto>
    {
        public ChoferesController(ICrudService<ChoferDto> service) : base(service) { }
    }

    // Clientes: también los vendedores pueden cargarlos y modificarlos.
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : CrudController<ClienteDto>
    {
        public ClientesController(ICrudService<ClienteDto> service) : base(service) { }

        protected override bool PuedeEscribir => true;
    }

    // Artículos: ABMC solo para el Administrador, más consultas y ajustes de stock.
    [ApiController]
    [Route("api/[controller]")]
    public class ArticulosController : CrudController<ArticuloDto>
    {
        private readonly IArticuloService _articulos;

        public ArticulosController(IArticuloService articulos) : base(articulos)
        {
            _articulos = articulos;
        }

        /// <summary>Artículos activos con stock en el mínimo o por debajo.</summary>
        [HttpGet("stock-bajo")]
        public async Task<ActionResult<IEnumerable<ArticuloDto>>> GetStockBajo()
        {
            return Ok(await _articulos.GetStockBajo());
        }

        /// <summary>Historial de movimientos de stock de un artículo.</summary>
        [HttpGet("{id:int}/movimientos")]
        public async Task<ActionResult<IEnumerable<MovimientoStockDto>>> GetMovimientos(int id)
        {
            return Ok(await _articulos.GetMovimientos(id));
        }

        /// <summary>Carga o descuenta stock manualmente (compra, ajuste, devolución).</summary>
        [HttpPost("{id:int}/ajustar-stock")]
        [Authorize(Roles = NombresRol.Administrador)]
        public async Task<ActionResult<ArticuloDto>> AjustarStock(int id, [FromBody] AjusteStockDto dto)
        {
            return Ok(await _articulos.AjustarStock(id, dto, UsuarioId));
        }
    }
}
