using System.Net;
using System.Text.Json;
using PlataformaFutevolei.Aplicacao.Excecoes;

namespace PlataformaFutevolei.Api.Middlewares;

public class MiddlewareTratamentoErros(RequestDelegate next, ILogger<MiddlewareTratamentoErros> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (Exception ex)
        {
            var (statusCode, mensagem) = ex switch
            {
                AcessoNegadoException => (HttpStatusCode.Forbidden, ex.Message),
                RegraNegocioException => (HttpStatusCode.BadRequest, ex.Message),
                EntidadeNaoEncontradaException => (HttpStatusCode.NotFound, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado.")
            };

            var correlationId = context.TraceIdentifier;

            logger.LogError(
                ex,
                "Erro na requisição. Metodo: {Metodo}. Caminho: {Caminho}. StatusCode: {StatusCode}. CorrelationId: {CorrelationId}. UsuarioId: {UsuarioId}.",
                context.Request.Method,
                context.Request.Path,
                (int)statusCode,
                correlationId,
                context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonimo");

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var resposta = ex switch
            {
                ConflitoGrupoAtletaException conflitoGrupoAtleta => JsonSerializer.Serialize(new
                {
                    erro = mensagem,
                    codigo = conflitoGrupoAtleta.Codigo,
                    grupoAtletaId = conflitoGrupoAtleta.GrupoAtletaId,
                    atletaId = conflitoGrupoAtleta.AtletaId,
                    correlationId
                }),
                _ => JsonSerializer.Serialize(new
                {
                    erro = mensagem,
                    correlationId
                })
            };

            await context.Response.WriteAsync(resposta);
        }
    }
}
