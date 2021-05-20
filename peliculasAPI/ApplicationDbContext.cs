using System;
using Microsoft.EntityFrameworkCore;
using peliculasAPI.Entidades;

namespace peliculasAPI
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Genero> Generos { get; set; }

        public DbSet<Actor> Actores { get; set; }
    }
}
