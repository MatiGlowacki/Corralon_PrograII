using Corralon.Logic.Models;
using Corralon.Logic.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Logic.Repositories
{
    public class UsuarioRepository : GeneralRepository<Usuarios>, IUsuarioRepository
    {
        public UsuarioRepository(CorralonDBContext context) : base(context)
        {
        }

        public async Task<Usuarios?> GetByNombreUsuario(string nombreUsuario)
        {
            return await _dbSet
                .Include(u => u.idRolNavigation)
                .FirstOrDefaultAsync(u => u.nombreUsuario == nombreUsuario);
        }
    }
}
