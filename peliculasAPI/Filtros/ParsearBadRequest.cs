using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace peliculasAPI.Filtros
{
    public class ParsearBadRequest : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var cast = context.Result as IStatusCodeActionResult;
            if (cast == null)
                return;

            if (cast.StatusCode == 400)
            {
                var respuesta = new List<string>();
                var resultadoActual = context.Result as BadRequestObjectResult;
                if (resultadoActual.Value is string)
                {
                    respuesta.Add(resultadoActual.Value.ToString());
                }
                else if (resultadoActual.Value is IEnumerable<IdentityError> errores)
                {
                    foreach (var error in errores)
                    {
                        respuesta.Add(error.Description);
                    }
                }
                else
                {
                    foreach (var llave in context.ModelState.Keys)
                    {
                        foreach (var error in context.ModelState[llave].Errors)
                        {
                            respuesta.Add($"{llave}: {error.ErrorMessage}");
                        }
                    }
                }
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}
