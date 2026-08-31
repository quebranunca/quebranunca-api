using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/grupos/{grupoId:guid}/presencas")]
public class GruposPresencasController(IPresencaGrupoServico presencaGrupoServico) : ControllerBase
{
    [HttpGet("painel")]
    [ProducesResponseType(typeof(PainelPresencaGrupoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPainel(Guid grupoId, CancellationToken cancellationToken)
    {
        var painel = await presencaGrupoServico.ObterPainelAsync(grupoId, cancellationToken);
        return Ok(painel);
    }
}
