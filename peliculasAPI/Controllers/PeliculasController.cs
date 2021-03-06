using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using peliculasAPI.DTOs;
using peliculasAPI.Entidades;
using peliculasAPI.Utilidades;

namespace peliculasAPI.Controllers
{
    [ApiController]
    [Route("api/peliculas")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "EsAdmin")]
    public class PeliculasController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly UserManager<IdentityUser> userManager;
        private readonly string contenedor = "peliculas";

        public PeliculasController(ApplicationDbContext context,
            IMapper mapper,
            IAlmacenadorArchivos almacenadorArchivos,
            UserManager<IdentityUser> userManager)
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.userManager = userManager;
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<PeliculaDTO>> Get(int id)
        {
            var pelicula = await context.Peliculas
                .Include(x => x.PeliculasGeneros).ThenInclude(x => x.Genero)
                .Include(x => x.PeliculasActores).ThenInclude(x => x.Actor)
                .Include(x => x.PeliculasCines).ThenInclude(x => x.Cine)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (pelicula == null)
            {
                return NotFound();
            }

            var promedioVoto = 0.0;
            var votoUsuario = 0;

            if (await context.Ratings.AnyAsync(x => x.PeliculaId == id))
            {
                promedioVoto = await context.Ratings
                    .Where(x => x.PeliculaId == id)
                    .AverageAsync(x => x.Puntuacion);

                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    var email = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
                    var usuario = await userManager.FindByEmailAsync(email);
                    var usuarioId = usuario.Id;

                    var ratingDb = await context.Ratings.FirstOrDefaultAsync(x => x.UsuarioId == usuarioId
                        && x.PeliculaId == id);

                    if (ratingDb != null)
                    {
                        votoUsuario = ratingDb.Puntuacion;
                    }
                }
            }

            var dto = mapper.Map<PeliculaDTO>(pelicula);

            dto.PromedioVoto = promedioVoto;
            dto.VotoUsuario = votoUsuario;

            dto.Actores = dto.Actores.OrderBy(x => x.Orden).ToList();
            return dto;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<LandingPageDTO>> Get()
        {
            var top = 6;
            var hoy = DateTime.Now;

            var proximosEstrenos = await context.Peliculas
                .Where(x => x.FechaLanzamiento > hoy)
                .OrderBy(x => x.FechaLanzamiento)
                .Take(top)
                .ToListAsync();

            var enCines = await context.Peliculas
                .Where(x => x.EnCines)
                .OrderBy(x => x.FechaLanzamiento)
                .Take(top)
                .ToListAsync();

            var resultado = new LandingPageDTO()
            {
                ProximosEstrenos = mapper.Map<List<PeliculaDTO>>(proximosEstrenos),
                EnCines = mapper.Map<List<PeliculaDTO>>(enCines)
            };
            return resultado;
        }

        [HttpGet("PutGet/{id:int}")]
        public async Task<ActionResult<PeliculasPutGetDTO>> PutGet(int id)
        {
            var peliculaActionResult = await Get(id);
            if (peliculaActionResult.Result is NotFoundResult)
            {
                return NotFound();
            }

            var pelicula = peliculaActionResult.Value;

            var generosSeleccionadoIds = pelicula.Generos.Select(x => x.Id).ToList();
            var generosNoSeleccionados = await context.Generos
                .Where(x => !generosSeleccionadoIds.Contains(x.Id))
                .ToListAsync();

            var cinesSeleccionadoIds = pelicula.Cines.Select(x => x.Id).ToList();
            var cinesNoSeleccionados = await context.Cines
                .Where(x => !cinesSeleccionadoIds.Contains(x.Id))
                .ToListAsync();

            var generosNoSeleccionadosDTO = mapper.Map<List<GeneroDTO>>(generosNoSeleccionados);
            var cinesNoSeleccionadosDTO = mapper.Map<List<CineDTO>>(cinesNoSeleccionados);

            var respuesta = new PeliculasPutGetDTO
            {
                Pelicula = pelicula,
                GenerosSeleccionados = pelicula.Generos,
                GenerosNoSeleccionados = generosNoSeleccionadosDTO,
                CinesSeleccionados = pelicula.Cines,
                CinesNoSeleccionados = cinesNoSeleccionadosDTO,
                Actores = pelicula.Actores
            };
            return respuesta;
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, [FromForm] PeliculaCreacionDTO peliculaCreacionDto)
        {
            var pelicula = await context.Peliculas
                .Include(x => x.PeliculasActores)
                .Include(x => x.PeliculasCines)
                .Include(x => x.PeliculasGeneros)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (pelicula == null)
            {
                return NotFound();
            }

            pelicula = mapper.Map(peliculaCreacionDto, pelicula);
            if (peliculaCreacionDto.Poster != null)
            {
                pelicula.Poster =
                    await almacenadorArchivos.EditarArchivo(contenedor, peliculaCreacionDto.Poster, pelicula.Poster);
            }

            EscribirOrdenActores(pelicula);

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("PostGet")]
        public async Task<ActionResult<PeliculasPostGetDTO>> PostGet()
        {
            var cines = await context.Cines.ToListAsync();
            var generos = await context.Generos.ToListAsync();

            var cinesDTO = mapper.Map<List<CineDTO>>(cines);
            var generosDTO = mapper.Map<List<GeneroDTO>>(generos);


            return new PeliculasPostGetDTO
            {
                Cines = cinesDTO,
                Generos = generosDTO
            };
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var pelicula = mapper.Map<Pelicula>(peliculaCreacionDTO);


            if (peliculaCreacionDTO.Poster != null)
            {
                pelicula.Poster = await almacenadorArchivos.GuardarArchivo(contenedor, peliculaCreacionDTO.Poster);
            }

            EscribirOrdenActores(pelicula);

            context.Add(pelicula);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private void EscribirOrdenActores(Pelicula pelicula)
        {
            if (pelicula.PeliculasActores != null)
            {
                for (int i = 0; i < pelicula.PeliculasActores.Count; i++)
                {
                    pelicula.PeliculasActores[i].Orden = i;
                }
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var pelicula = await context.Peliculas.FirstOrDefaultAsync(x => x.Id == id);
            if (pelicula == null)
            {
                return NotFound();
            }

            context.Remove(pelicula);
            await context.SaveChangesAsync();
            await almacenadorArchivos.BorrarArchivo(pelicula.Poster, contenedor);
            return NoContent();
        }

        [HttpGet("filtrar")]
        public async Task<ActionResult<List<PeliculaDTO>>> Filtrar([FromQuery] PeliculaFiltrarDTO peliculaFiltrarDto)
        {
            var peliculasQueryable = context.Peliculas.AsQueryable();
            if (!string.IsNullOrEmpty(peliculaFiltrarDto.Titulo))
            {
                peliculasQueryable =
                    peliculasQueryable.Where(pelicula => pelicula.Titulo.Contains(peliculaFiltrarDto.Titulo));
            }

            if (peliculaFiltrarDto.EnCines)
            {
                peliculasQueryable = peliculasQueryable.Where(pelicula => pelicula.EnCines);
            }

            if (peliculaFiltrarDto.ProximosEstrenos)
            {
                var hoy = DateTime.Now;
                peliculasQueryable = peliculasQueryable.Where(pelicula => pelicula.FechaLanzamiento > hoy);
            }

            if (peliculaFiltrarDto.GeneroId != 0)
            {
                peliculasQueryable = peliculasQueryable
                    .Where(pelicula =>
                        pelicula.PeliculasGeneros.Select(y => y.GeneroId).Contains(peliculaFiltrarDto.GeneroId));
            }

            await HttpContext.IntertarParametrosPaginacionEnCabecera(peliculasQueryable);

            var peliculas = await peliculasQueryable.Paginar(peliculaFiltrarDto.PaginacionDTO).ToListAsync();

            return mapper.Map<List<PeliculaDTO>>(peliculas);
        }
    }
}