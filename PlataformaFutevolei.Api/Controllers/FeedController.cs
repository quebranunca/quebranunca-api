using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/feed")]
public class FeedController(IPartidaServico partidaServico) : ControllerBase
{
    [HttpGet("partidas")]
    [ProducesResponseType(typeof(FeedPartidasRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPartidas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var feed = await partidaServico.ListarFeedAsync(page, pageSize, cancellationToken);
        return Ok(feed);
    }
}
