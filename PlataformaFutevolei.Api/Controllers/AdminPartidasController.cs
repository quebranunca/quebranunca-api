using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/partidas")]
public class AdminPartidasController(IPartidaCancelamentoServico partidaCancelamentoServico) : ControllerBase
{
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ExcluirDefinitivamente(
        Guid id,
        [FromBody] ExcluirPartidaDefinitivamenteDto? dto,
        CancellationToken cancellationToken)
    {
        await partidaCancelamentoServico.ExcluirDefinitivamenteAsync(
            id,
            dto ?? new ExcluirPartidaDefinitivamenteDto(null),
            cancellationToken);
        return NoContent();
    }
}
