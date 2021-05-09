using System;
using System.ComponentModel.DataAnnotations;
using peliculasAPI.Entidades.Validaciones;

namespace peliculasAPI.DTOs
{
    public class GeneroCreacionDTO
    {
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [StringLength(maximumLength: 50)]
        [PrimeraLetraMayuscula]
        public string Nombre { get; set; }
    }
}
