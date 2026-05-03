using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/grupos")]
public class GruposController(IGrupoResumoUsuarioServico grupoResumoUsuarioServico) : ControllerBase
{
    [HttpGet("resumo-usuario")]
    [ProducesResponseType(typeof(GrupoResumoUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumoUsuario(CancellationToken cancellationToken)
    {
        var resumo = await grupoResumoUsuarioServico.ObterMeuResumoAsync(cancellationToken);
        return Ok(resumo);
    }
}
