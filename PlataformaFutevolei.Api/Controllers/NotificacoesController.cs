using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificacoes")]
public class NotificacoesController(INotificacaoUsuarioServico notificacaoServico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacaoUsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] bool somenteNaoLidas = false,
        [FromQuery] int limite = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await notificacaoServico.ListarMinhasAsync(somenteNaoLidas, limite, cancellationToken));

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(NotificacoesResumoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo(CancellationToken cancellationToken) =>
        Ok(await notificacaoServico.ObterResumoAsync(cancellationToken));

    [HttpPost("{id:guid}/ler")]
    [ProducesResponseType(typeof(NotificacaoUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarcarComoLida(Guid id, CancellationToken cancellationToken) =>
        Ok(await notificacaoServico.MarcarComoLidaAsync(id, cancellationToken));

    [HttpPost("ler-todas")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarcarTodasComoLidas(CancellationToken cancellationToken)
    {
        await notificacaoServico.MarcarTodasComoLidasAsync(cancellationToken);
        return NoContent();
    }
}
