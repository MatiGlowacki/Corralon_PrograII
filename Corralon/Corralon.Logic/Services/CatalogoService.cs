using Corralon.Logic.DTOs;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    /// <summary>Listas de solo lectura para completar los combos del front.</summary>
    public interface ICatalogoService
    {
        Task<IEnumerable<LookupDto>> Provincias();
        Task<IEnumerable<LocalidadDto>> Localidades(int? idProvincia);
        Task<IEnumerable<LookupDto>> CondicionesIva();
        Task<IEnumerable<UnidadMedidaDto>> UnidadesMedida();
        Task<IEnumerable<LookupDto>> Roles();
    }

    public class CatalogoService : ICatalogoService
    {
        private readonly IUnitOfWork _uow;

        public CatalogoService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<LookupDto>> Provincias()
        {
            return await _uow.Provincias.Query().AsNoTracking()
                .OrderBy(p => p.nombre)
                .Select(p => new LookupDto { Id = p.idProvincia, Nombre = p.nombre })
                .ToListAsync();
        }

        public async Task<IEnumerable<LocalidadDto>> Localidades(int? idProvincia)
        {
            var consulta = _uow.Localidades.Query().AsNoTracking();
            if (idProvincia.HasValue)
            {
                var prov = idProvincia.Value;
                consulta = consulta.Where(l => l.idProvincia == prov);
            }

            return await consulta
                .OrderBy(l => l.nombre)
                .Select(l => new LocalidadDto
                {
                    IdLocalidad = l.idLocalidad,
                    Nombre = l.nombre,
                    CodigoPostal = l.codigoPostal,
                    IdProvincia = l.idProvincia,
                    Provincia = l.idProvinciaNavigation.nombre
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<LookupDto>> CondicionesIva()
        {
            return await _uow.CondicionesIva.Query().AsNoTracking()
                .OrderBy(c => c.idCondicionIva)
                .Select(c => new LookupDto { Id = c.idCondicionIva, Nombre = c.nombre })
                .ToListAsync();
        }

        public async Task<IEnumerable<UnidadMedidaDto>> UnidadesMedida()
        {
            return await _uow.UnidadesMedida.Query().AsNoTracking()
                .OrderBy(u => u.nombre)
                .Select(u => new UnidadMedidaDto
                {
                    IdUnidadMedida = u.idUnidadMedida,
                    Nombre = u.nombre,
                    Abreviatura = u.abreviatura,
                    PermiteDecimales = u.permiteDecimales
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<LookupDto>> Roles()
        {
            return await _uow.Roles.Query().AsNoTracking()
                .OrderBy(r => r.nombre)
                .Select(r => new LookupDto { Id = r.idRol, Nombre = r.nombre })
                .ToListAsync();
        }
    }
}
