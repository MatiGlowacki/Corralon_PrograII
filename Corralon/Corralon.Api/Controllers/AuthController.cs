using System.Security.Claims;
using Corralon.Api.Infrastructure;
using Corralon.Logic.DTOs;
using Corralon.Logic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _auth;
        private readonly ITokenService _tokens;

        public AuthController(IAuthService auth, ITokenService tokens)
        {
            _auth = auth;
            _tokens = tokens;
        }

        /// <summary>Valida usuario y contraseña y devuelve el token JWT.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginRespuestaDto>> Login([FromBody] LoginDto dto)
        {
            var usuario = await _auth.Login(dto);
            var (token, expira) = _tokens.Generar(usuario);

            return Ok(new LoginRespuestaDto
            {
                Token = token,
                Expira = expira,
                IdUsuario = usuario.IdUsuario,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Rol = usuario.Rol
            });
        }

        /// <summary>Alta de usuarios (solo Administrador).</summary>
        [HttpPost("registrar")]
        [Authorize(Roles = NombresRol.Administrador)]
        public async Task<ActionResult<UsuarioAutenticadoDto>> Registrar([FromBody] RegistrarUsuarioDto dto)
        {
            var usuario = await _auth.Registrar(dto);
            return StatusCode(StatusCodes.Status201Created, usuario);
        }

        /// <summary>Datos del usuario logueado, tomados del token.</summary>
        [HttpGet("yo")]
        [Authorize]
        public ActionResult<UsuarioAutenticadoDto> Yo()
        {
            return Ok(new UsuarioAutenticadoDto
            {
                IdUsuario = UsuarioId,
                NombreUsuario = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                NombreCompleto = User.FindFirstValue("nombreCompleto") ?? string.Empty,
                Rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty
            });
        }
    }
}
