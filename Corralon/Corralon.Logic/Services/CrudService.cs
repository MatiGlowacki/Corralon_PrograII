using Corralon.Logic.Exceptions;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Services
{
    /// <summary>ABMC estándar para las tablas con baja lógica (columna "activo").</summary>
    public interface ICrudService<TDto> where TDto : class
    {
        Task<IEnumerable<TDto>> GetAll(bool soloActivos);
        Task<TDto> GetById(int id);
        Task<TDto> Create(TDto dto, int idUsuario);
        Task<TDto> Update(int id, TDto dto, int idUsuario);
        Task Delete(int id);
    }

    /// <summary>
    /// Base para los servicios de ABMC. Cada servicio concreto solo define cómo
    /// pasar de entidad a DTO y viceversa, y sus validaciones propias.
    /// </summary>
    public abstract class CrudService<TEntity, TDto> : ICrudService<TDto>
        where TEntity : class
        where TDto : class
    {
        protected readonly IUnitOfWork _uow;
        protected readonly IRepository<TEntity> _repo;

        protected CrudService(IUnitOfWork uow, IRepository<TEntity> repo)
        {
            _uow = uow;
            _repo = repo;
        }

        protected abstract string NombreEntidad { get; }
        protected abstract string ClavePrimaria { get; }
        protected abstract TEntity Nuevo();
        protected abstract void Aplicar(TDto dto, TEntity entidad);
        protected abstract TDto ADto(TEntity entidad);
        protected abstract int ObtenerId(TEntity entidad);
        protected abstract void SetActivo(TEntity entidad, bool activo);

        protected virtual IQueryable<TEntity> ConIncludes(IQueryable<TEntity> consulta) => consulta;
        protected virtual IQueryable<TEntity> Ordenar(IQueryable<TEntity> consulta) => consulta;
        protected virtual Task Validar(TDto dto, int? id) => Task.CompletedTask;
        protected virtual Task DespuesDeCrear(TEntity entidad, TDto dto, int idUsuario) => Task.CompletedTask;

        public async Task<IEnumerable<TDto>> GetAll(bool soloActivos)
        {
            var consulta = ConIncludes(_repo.Query()).AsNoTracking();
            if (soloActivos)
            {
                consulta = consulta.Where(e => EF.Property<bool>(e, "activo"));
            }

            var lista = await Ordenar(consulta).ToListAsync();
            return lista.Select(ADto).ToList();
        }

        public async Task<TDto> GetById(int id)
        {
            var pk = ClavePrimaria;
            var entidad = await ConIncludes(_repo.Query())
                .AsNoTracking()
                .FirstOrDefaultAsync(e => EF.Property<int>(e, pk) == id);

            if (entidad == null)
            {
                throw new NotFoundException($"{NombreEntidad} {id} no existe.");
            }
            return ADto(entidad);
        }

        public async Task<TDto> Create(TDto dto, int idUsuario)
        {
            await Validar(dto, null);

            return await _uow.Transaccion(async () =>
            {
                var entidad = Nuevo();
                Aplicar(dto, entidad);

                await _repo.Add(entidad);
                await _uow.SaveChanges();
                await DespuesDeCrear(entidad, dto, idUsuario);

                return await GetById(ObtenerId(entidad));
            });
        }

        public async Task<TDto> Update(int id, TDto dto, int idUsuario)
        {
            await Validar(dto, id);

            var entidad = await _repo.GetById(id);
            if (entidad == null)
            {
                throw new NotFoundException($"{NombreEntidad} {id} no existe.");
            }

            Aplicar(dto, entidad);
            await _uow.SaveChanges();

            return await GetById(id);
        }

        public async Task Delete(int id)
        {
            var entidad = await _repo.GetById(id);
            if (entidad == null)
            {
                throw new NotFoundException($"{NombreEntidad} {id} no existe.");
            }

            // Baja lógica: el registro puede estar referenciado por facturas o repartos viejos.
            SetActivo(entidad, false);
            await _uow.SaveChanges();
        }
    }
}
