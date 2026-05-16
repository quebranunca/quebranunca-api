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
}
