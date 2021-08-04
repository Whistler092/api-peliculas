using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using peliculasAPI.DTOs;

namespace peliculasAPI.Controllers
{
    [Route("api/cuentas")]
    [ApiController]
    public class CuentasController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IConfiguration configuration;

        public CuentasController(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }
        
        [HttpPost("crear")]
        public async Task<ActionResult<RespuestaAutenticacion>> Crear([FromBody] CredencialesUsuario credenciales)
        {
            var usuario = new IdentityUser
            {
                UserName = credenciales.Email,
                Email = credenciales.Email
            };
            var resultado = await userManager.CreateAsync(usuario, credenciales.Password);

            if (resultado.Succeeded)
            {
                return await Construirtoken(credenciales);
            }
            else
            {
                return BadRequest(resultado.Errors);
            }

        }

        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacion>> Login([FromBody] CredencialesUsuario credenciales)
        {
            var result = await signInManager.PasswordSignInAsync(credenciales.Email, credenciales.Password,
                isPersistent: false, lockoutOnFailure: false);

            return result.Succeeded ? await Construirtoken(credenciales) : BadRequest("Login Incorrecto");
        }

        private async Task<RespuestaAutenticacion> Construirtoken(CredencialesUsuario credenciales)
        {
            var claims = new List<Claim>()
            {
                new Claim("email", credenciales.Email),
            };
            var usuario = await userManager.FindByEmailAsync(credenciales.Email);
            var claimsDb = await userManager.GetClaimsAsync(usuario);
            
            claims.AddRange(claimsDb);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["llavejwt"]));
            var creds = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddDays(8);

            var token = new JwtSecurityToken(issuer: null, audience: null, claims: claims, expires: expiracion,
                signingCredentials: creds);
            
            return new RespuestaAutenticacion()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiracion = expiracion
            };
        }

    }
}