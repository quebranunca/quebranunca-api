using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlataformaFutevolei.Api.Configuracao;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting(ConfiguracaoRateLimiting.PoliticaAcesso)]
[Route("api/presencas-grupo")]
public class PresencasGrupoController(IPresencaGrupoServico presencaGrupoServico) : ControllerBase
{
    [HttpPost("consultar")]
    [ProducesResponseType(typeof(ConfirmacaoPresencaGrupoPublicaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Consultar(
        [FromBody] ConsultarConfirmacaoPresencaGrupoDto dto,
        CancellationToken cancellationToken)
    {
        var confirmacao = await presencaGrupoServico.ConsultarAsync(dto.Codigo, cancellationToken);
        return Ok(confirmacao);
    }

    [HttpPost("responder")]
    [ProducesResponseType(typeof(ConfirmacaoPresencaGrupoPublicaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Responder(
        [FromBody] ResponderConfirmacaoPresencaGrupoDto dto,
        CancellationToken cancellationToken)
    {
        var confirmacao = await presencaGrupoServico.ResponderAsync(
            dto.Codigo,
            dto.VaiParticipar,
            cancellationToken);
        return Ok(confirmacao);
    }
}
