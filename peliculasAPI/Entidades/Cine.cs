using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace peliculasAPI.Entidades
{
    public class Cine
    {
        public int Id { get; set; }

        [Required]
        [StringLength(maximumLength: 75)]
        public string Nombre { get; set; }

        public Point Ubicacion { get; set; }

        public List<PeliculasCines> PeliculasCines { get; set; }
    }
}
