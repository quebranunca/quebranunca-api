using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Route("api/termos")]
public class TermosController(IPrivacidadeServico privacidadeServico) : ControllerBase
{
    [HttpGet("versao-atual")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TermosVersaoAtualDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterVersaoAtual(CancellationToken cancellationToken)
    {
        var termos = await privacidadeServico.ObterTermosVersaoAtualAsync(cancellationToken);
        return Ok(termos);
    }
}
