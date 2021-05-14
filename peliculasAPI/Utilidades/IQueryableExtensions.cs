using System.Linq;
using peliculasAPI.DTOs;

namespace peliculasAPI.Utilidades
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable, PaginacionDTO paginationDTO)
        {
            return queryable
                .Skip((paginationDTO.Pagina - 1) * paginationDTO.RecordsPorPagina)
                .Take(paginationDTO.RecordsPorPagina);
        }
    }
}
