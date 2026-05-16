using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController(IDashboardAtletaServico dashboardAtletaServico) : ControllerBase
{
    [HttpGet("atleta")]
    [ProducesResponseType(typeof(DashboardAtletaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDashboardAtleta(CancellationToken cancellationToken)
    {
        var dashboard = await dashboardAtletaServico.ObterDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }
}
