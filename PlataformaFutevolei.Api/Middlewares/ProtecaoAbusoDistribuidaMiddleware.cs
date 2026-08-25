using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;

namespace PlataformaFutevolei.Api.Middlewares;

public sealed class ProtecaoAbusoDistribuidaMiddleware(RequestDelegate next)
{
    private const int LimiteCorpoBytes = 32 * 1024;

    public async Task InvokeAsync(HttpContext context, IProtecaoAbusoDistribuida protecao, IConfiguration configuration)
    {
        var regra = ObterRegra(context.Request.Path, context.Request.Method, configuration);
        if (regra is null)
        {
            await next(context);
            return;
        }

        var identificador = await ObterIdentificadorAsync(context.Request, context.RequestAborted);
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var chaves = new List<string> { GerarChave(regra.Value.Politica, "ip", ip) };
        if (!string.IsNullOrWhiteSpace(identificador))
        {
            chaves.Add(GerarChave(regra.Value.Politica, "identificador", identificador));
        }

        foreach (var chave in chaves)
        {
            if (!await protecao.TentarConsumirAsync(chave, regra.Value.Limite, regra.Value.Janela, context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    titulo = "Muitas tentativas.",
                    detalhe = "Aguarde alguns instantes antes de tentar novamente.",
                    status = StatusCodes.Status429TooManyRequests,
                    correlationId = context.TraceIdentifier
                }, context.RequestAborted);
                return;
            }
        }

        await next(context);
    }

    private static (string Politica, int Limite, TimeSpan Janela)? ObterRegra(
        PathString path,
        string metodo,
        IConfiguration configuration)
    {
        if (!HttpMethods.IsPost(metodo)) return null;
        var valor = path.Value?.ToLowerInvariant();
        return valor switch
        {
            "/api/autenticacao/login" or
            "/api/autenticacao/confirmar-codigo" or
            "/api/autenticacao/login/codigo" or
            "/api/autenticacao/esqueci-senha/redefinir" or
            "/api/autenticacao/criar-senha-com-token"
                => ("acesso", configuration.GetValue("ProtecaoAbuso:AcessoPorMinuto", 15), TimeSpan.FromMinutes(1)),
            "/api/autenticacao/iniciar-acesso" or
            "/api/autenticacao/login/codigo/solicitar" or
            "/api/autenticacao/esqueci-senha/solicitar"
                => ("envio-codigo", configuration.GetValue("ProtecaoAbuso:CodigosPorDezMinutos", 3), TimeSpan.FromMinutes(10)),
            "/api/autenticacao/registrar" or
            "/api/autenticacao/registrar-por-convite" or
            "/api/autenticacao/cadastro-publico/senha" or
            "/api/autenticacao/completar-cadastro-publico"
                => ("cadastro", configuration.GetValue("ProtecaoAbuso:CadastroPorDezMinutos", 5), TimeSpan.FromMinutes(10)),
            _ => null
        };
    }

    private static async Task<string?> ObterIdentificadorAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is > LimiteCorpoBytes or <= 0) return null;
        request.EnableBuffering();
        try
        {
            using var documento = await JsonDocument.ParseAsync(
                request.Body,
                new JsonDocumentOptions { MaxDepth = 8 },
                cancellationToken);
            if (documento.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var nome in new[] { "email", "Email" })
            {
                if (documento.RootElement.TryGetProperty(nome, out var campo) && campo.ValueKind == JsonValueKind.String)
                {
                    return campo.GetString()?.Trim().ToLowerInvariant();
                }
            }
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static string GerarChave(string politica, string tipo, string valor)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
        return $"{politica}:{tipo}:{Convert.ToHexString(hash)}";
    }
}
