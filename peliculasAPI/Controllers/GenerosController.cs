using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using peliculasAPI.Entidades;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace peliculasAPI.Controllers
{
    [Route("api/generos")]
    [ApiController]
    public class GenerosController : ControllerBase
    {

        private readonly ILogger<GenerosController> logger;

        public GenerosController(ILogger<GenerosController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        public ActionResult<List<Genero>> Get()
        {
            return new List<Genero>()
            {
                new Genero{Id = 1, Nombre = "Comedia"}
            };
        }

        
    }
}
