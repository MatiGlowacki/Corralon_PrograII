using Corralon.Logic.DTOs;
using Corralon.Logic.Exceptions;
using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    public static class NombresRol
    {
        public const string Administrador = "Administrador";
        public const string Vendedor = "Vendedor";
    }

    public interface IAuthService
    {
        /// <summary>Valida las credenciales. Si son incorrectas lanza UnauthorizedAccessException.</summary>
        Task<UsuarioAutenticadoDto> Login(LoginDto dto);

        Task<UsuarioAutenticadoDto> Registrar(RegistrarUsuarioDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;

        public AuthService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<UsuarioAutenticadoDto> Login(LoginDto dto)
        {
            var usuario = await _uow.Usuarios.GetByNombreUsuario(dto.NombreUsuario.Trim());

            // Mismo mensaje para usuario inexistente o clave incorrecta: no se da pistas.
            if (usuario == null || !usuario.activo || !PasswordHasher.Verificar(dto.Password, usuario.passwordHash))
            {
                throw new UnauthorizedAccessException("Usuario o contraseña incorrectos.");
            }

            usuario.ultimoAcceso = DateTime.Now;
            await _uow.SaveChanges();

            return ADto(usuario);
        }

        public async Task<UsuarioAutenticadoDto> Registrar(RegistrarUsuarioDto dto)
        {
            var nombre = dto.NombreUsuario.Trim();
            if (await _uow.Usuarios.Query().AnyAsync(u => u.nombreUsuario == nombre))
            {
                throw new BusinessException($"El usuario '{nombre}' ya existe.");
            }

            var idRol = dto.IdRol;
            if (!await _uow.Roles.Query().AnyAsync(r => r.idRol == idRol))
            {
                throw new BusinessException("El rol indicado no existe.");
            }

            var usuario = new Usuarios
            {
                nombreUsuario = nombre,
                passwordHash = PasswordHasher.Hash(dto.Password),
                nombreCompleto = dto.NombreCompleto.Trim(),
                email = dto.Email?.Trim(),
                idRol = dto.IdRol,
                activo = true
            };

            await _uow.Usuarios.Add(usuario);
            await _uow.SaveChanges();

            var creado = await _uow.Usuarios.GetByNombreUsuario(nombre);
            return ADto(creado!);
        }

        private static UsuarioAutenticadoDto ADto(Usuarios u) => new UsuarioAutenticadoDto
        {
            IdUsuario = u.idUsuario,
            NombreUsuario = u.nombreUsuario,
            NombreCompleto = u.nombreCompleto,
            Rol = u.idRolNavigation.nombre
        };
    }
}
