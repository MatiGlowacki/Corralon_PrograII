using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Corralon.Logic.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Corralon.Api.Infrastructure
{
    public interface ITokenService
    {
        (string Token, DateTime Expira) Generar(UsuarioAutenticadoDto usuario);
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public (string Token, DateTime Expira) Generar(UsuarioAutenticadoDto usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("nombreCompleto", usuario.NombreCompleto)
            };

            var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
            var expira = DateTime.UtcNow.AddMinutes(_settings.ExpiraMinutos);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expira,
                signingCredentials: credenciales);

            return (new JwtSecurityTokenHandler().WriteToken(token), expira);
        }
    }
}
