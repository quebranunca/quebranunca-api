using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PlataformaFutevolei.Api.Seguranca;

namespace PlataformaFutevolei.Api.Configuracao;

public static class ConfiguracaoRateLimiting
{
    public const string PoliticaAcesso = "acesso";
    public const string PoliticaCadastro = "cadastro";
    public const string PoliticaEnvioCodigo = "envio-codigo";
    public const string PoliticaConvites = "convites";

    public static IServiceCollection AdicionarRateLimitingProtecaoAbuso(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var acessoPorMinuto = ObterLimite(configuration, "ProtecaoAbuso:AcessoPorMinuto", 15);
        var cadastroPorDezMinutos = ObterLimite(configuration, "ProtecaoAbuso:CadastroPorDezMinutos", 5);
        var codigosPorDezMinutos = ObterLimite(configuration, "ProtecaoAbuso:CodigosPorDezMinutos", 3);
        var convitesPorMinuto = ObterLimite(configuration, "ProtecaoAbuso:ConvitesPorMinuto", 10);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = ResponderRejeicaoAsync;

            options.AddPolicy(PoliticaAcesso, context => CriarLimitadorPorIp(
                context,
                PoliticaAcesso,
                acessoPorMinuto,
                TimeSpan.FromMinutes(1)));

            options.AddPolicy(PoliticaCadastro, context => CriarLimitadorPorIp(
                context,
                PoliticaCadastro,
                cadastroPorDezMinutos,
                TimeSpan.FromMinutes(10)));

            options.AddPolicy(PoliticaEnvioCodigo, context => CriarLimitadorPorIp(
                context,
                PoliticaEnvioCodigo,
                codigosPorDezMinutos,
                TimeSpan.FromMinutes(10)));

            options.AddPolicy(PoliticaConvites, context => CriarLimitadorPorUsuarioOuIp(
                context,
                PoliticaConvites,
                convitesPorMinuto,
                TimeSpan.FromMinutes(1)));
        });

        return services;
    }

    private static RateLimitPartition<string> CriarLimitadorPorIp(
        HttpContext context,
        string politica,
        int limite,
        TimeSpan janela)
    {
        var ip = EnderecoIpClienteHttp.Obter(context) ?? "desconhecido";
        return CriarLimitador($"{politica}:ip:{ip}", limite, janela);
    }

    private static RateLimitPartition<string> CriarLimitadorPorUsuarioOuIp(
        HttpContext context,
        string politica,
        int limite,
        TimeSpan janela)
    {
        var usuarioId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var particao = string.IsNullOrWhiteSpace(usuarioId)
            ? $"ip:{EnderecoIpClienteHttp.Obter(context) ?? "desconhecido"}"
            : $"usuario:{usuarioId}";

        return CriarLimitador($"{politica}:{particao}", limite, janela);
    }

    private static RateLimitPartition<string> CriarLimitador(string chave, int limite, TimeSpan janela)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            chave,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = limite,
                QueueLimit = 0,
                Window = janela
            });
    }

    private static async ValueTask ResponderRejeicaoAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ProtecaoAbuso");
        logger.LogWarning(
            "Requisição rejeitada por limite de taxa. Rota={Rota}; CorrelationId={CorrelationId}.",
            context.HttpContext.Request.Path.Value,
            context.HttpContext.TraceIdentifier);

        await response.WriteAsJsonAsync(
            new
            {
                titulo = "Muitas tentativas.",
                detalhe = "Aguarde alguns instantes antes de tentar novamente.",
                status = StatusCodes.Status429TooManyRequests,
                correlationId = context.HttpContext.TraceIdentifier
            },
            cancellationToken);
    }

    private static int ObterLimite(IConfiguration configuration, string chave, int padrao)
    {
        var valor = configuration.GetValue<int?>(chave);
        return valor is > 0 ? valor.Value : padrao;
    }
}
