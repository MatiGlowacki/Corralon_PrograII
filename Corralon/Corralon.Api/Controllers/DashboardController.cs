using Corralon.Logic.DTOs;
using Corralon.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ApiControllerBase
    {
        private readonly IDashboardService _dashboard;

        public DashboardController(IDashboardService dashboard)
        {
            _dashboard = dashboard;
        }

        /// <summary>Información resumida del mes en curso.</summary>
        [HttpGet]
        public async Task<ActionResult<DashboardDto>> Get()
        {
            return Ok(await _dashboard.Obtener());
        }
    }
}
