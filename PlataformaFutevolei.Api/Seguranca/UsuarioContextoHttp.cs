using System.Security.Claims;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;

namespace PlataformaFutevolei.Api.Seguranca;

public class UsuarioContextoHttp(IHttpContextAccessor httpContextAccessor) : IUsuarioContexto
{
    public Guid? UsuarioId
    {
        get
        {
            var valor = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(valor, out var usuarioId) ? usuarioId : null;
        }
    }
}
