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
        catch (Exception ex)
        {
            var (statusCode, mensagem) = ex switch
            {
                RegraNegocioException => (HttpStatusCode.BadRequest, ex.Message),
                EntidadeNaoEncontradaException => (HttpStatusCode.NotFound, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado.")
            };

            var traceId = context.TraceIdentifier;

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                logger.LogError(
                    ex,
                    "Erro não tratado na aplicação. Metodo: {Metodo}. Caminho: {Caminho}. TraceId: {TraceId}. Usuario: {Usuario}.",
                    context.Request.Method,
                    context.Request.Path,
                    traceId,
                    context.User?.Identity?.Name ?? "anonimo");
            }

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";
            var resposta = JsonSerializer.Serialize(new
            {
                erro = mensagem,
                traceId
            });

            await context.Response.WriteAsync(resposta);
        }
    }
}
