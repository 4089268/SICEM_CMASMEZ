using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sicem_Blazor.Helpers;

namespace Sicem_Blazor.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class ApiAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var authOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<ApiAuthSettings>>();
        var authToken = authOptions.Value.Token;

        // * get the token
        var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Result = new JsonResult(new { message = "Unauthorized" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }
        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Validate token
        if (token != authToken)
        {
            context.Result = new JsonResult(new { message = "Unauthorized" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }
    }
}