using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VibrationMonitorReservation.Services
{
    //Toiminto Swagger debugausta varten. Lisää poikkeuksen, jolla halutuista API-pyynnöistä jätetään tokenin käyttö pois.
    //Tokenia ei vaadita rekisteröityessä ja kirjautuessa Swaggeria käytettäessä. Poikkeus lisätty [AllowAnonymous] merkinnällä.
    public class UnauthenticatedOperationFilterForSwagger : IOperationFilter    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<AuthorizeAttribute>();

            if (!authAttributes.Any())
            {
                operation.Security = new List<OpenApiSecurityRequirement>();
            }
        }
    }
}
