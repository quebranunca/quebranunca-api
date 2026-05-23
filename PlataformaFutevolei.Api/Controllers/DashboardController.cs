using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController(
    IDashboardAtletaServico dashboardAtletaServico,
    IDashboardPublicoServico dashboardPublicoServico
) : ControllerBase
{
    [HttpGet("publico")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DashboardPublicoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDashboardPublico(CancellationToken cancellationToken)
    {
        var dashboard = await dashboardPublicoServico.ObterDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("atleta")]
    [ProducesResponseType(typeof(DashboardAtletaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDashboardAtleta(CancellationToken cancellationToken)
    {
        var dashboard = await dashboardAtletaServico.ObterDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("atleta/perfil")]
    [ProducesResponseType(typeof(DashboardAtletaPerfilDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPerfilAtleta(CancellationToken cancellationToken)
    {
        var perfil = await dashboardAtletaServico.ObterPerfilAsync(cancellationToken);
        return Ok(perfil);
    }

    [HttpGet("atleta/resumo")]
    [ProducesResponseType(typeof(DashboardAtletaResumoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumoAtleta(CancellationToken cancellationToken)
    {
        var resumo = await dashboardAtletaServico.ObterResumoAsync(cancellationToken);
        return Ok(resumo);
    }

    [HttpGet("atleta/insights")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterInsightsAtleta(CancellationToken cancellationToken)
    {
        var insights = await dashboardAtletaServico.ObterInsightsAsync(cancellationToken);
        return Ok(insights);
    }

    [HttpGet("atleta/ultimas-partidas")]
    [ProducesResponseType(typeof(IReadOnlyList<DashboardAtletaPartidaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarUltimasPartidasAtleta(CancellationToken cancellationToken)
    {
        var partidas = await dashboardAtletaServico.ListarUltimasPartidasAsync(cancellationToken);
        return Ok(partidas);
    }

    [HttpGet("atleta/conexoes")]
    [ProducesResponseType(typeof(DashboardAtletaConexoesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterConexoesAtleta(CancellationToken cancellationToken)
    {
        var conexoes = await dashboardAtletaServico.ObterConexoesAsync(cancellationToken);
        return Ok(conexoes);
    }

    [HttpGet("atleta/frequencia")]
    [ProducesResponseType(typeof(IReadOnlyList<DashboardAtletaHeatmapDiaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterFrequenciaAtleta(CancellationToken cancellationToken)
    {
        var frequencia = await dashboardAtletaServico.ObterFrequenciaAsync(cancellationToken);
        return Ok(frequencia);
    }
}
