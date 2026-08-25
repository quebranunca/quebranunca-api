using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;

namespace PlataformaFutevolei.Api.Seguranca;

public sealed class SessaoRequisicaoContextoHttp(IHttpContextAccessor httpContextAccessor) : ISessaoRequisicaoContexto
{
    public string? IpAddress => httpContextAccessor.HttpContext is { } context
        ? EnderecoIpClienteHttp.Obter(context)
        : null;

    public string? UserAgent
    {
        get
        {
            var valor = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(valor) ? null : valor[..Math.Min(valor.Length, 500)];
        }
    }
}
