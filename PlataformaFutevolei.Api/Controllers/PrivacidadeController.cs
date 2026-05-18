using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Route("api/privacidade")]
public class PrivacidadeController(IPrivacidadeServico privacidadeServico) : ControllerBase
{
    [HttpGet("politica-atual")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PoliticaPrivacidadeAtualDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPoliticaAtual(CancellationToken cancellationToken)
    {
        var politica = await privacidadeServico.ObterPoliticaAtualAsync(cancellationToken);
        return Ok(politica);
    }

    [HttpGet("minhas-preferencias")]
    [Authorize]
    [ProducesResponseType(typeof(PreferenciasPrivacidadeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterMinhasPreferencias(CancellationToken cancellationToken)
    {
        var preferencias = await privacidadeServico.ObterMinhasPreferenciasAsync(cancellationToken);
        return Ok(preferencias);
    }

    [HttpPut("minhas-preferencias")]
    [Authorize]
    [ProducesResponseType(typeof(PreferenciasPrivacidadeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarMinhasPreferencias(
        [FromBody] AtualizarPreferenciasPrivacidadeDto dto,
        CancellationToken cancellationToken)
    {
        var preferencias = await privacidadeServico.AtualizarMinhasPreferenciasAsync(dto, cancellationToken);
        return Ok(preferencias);
    }

    [HttpPost("consentimentos")]
    [Authorize]
    [ProducesResponseType(typeof(PreferenciasPrivacidadeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegistrarConsentimento(
        [FromBody] RegistrarConsentimentoLgpdDto dto,
        CancellationToken cancellationToken)
    {
        var dtoComAuditoria = dto with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        var preferencias = await privacidadeServico.RegistrarConsentimentoAsync(dtoComAuditoria, cancellationToken);
        return Ok(preferencias);
    }

    [HttpPost("solicitar-exclusao")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SolicitarExclusao(CancellationToken cancellationToken)
    {
        await privacidadeServico.SolicitarExclusaoContaAsync(cancellationToken);
        return NoContent();
    }
}
