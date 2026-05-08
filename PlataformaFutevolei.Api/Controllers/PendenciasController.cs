using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pendencias")]
public class PendenciasController(IPendenciaServico pendenciaServico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PendenciaUsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var pendencias = await pendenciaServico.ListarMinhasAsync(cancellationToken);
        return Ok(pendencias);
    }

    [HttpGet("existe")]
    [ProducesResponseType(typeof(IReadOnlyList<PendenciaUsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Existe(CancellationToken cancellationToken)
    {
        var existePendencia = await pendenciaServico.ExistePendenciaAsync(cancellationToken);
        return Ok(existePendencia);
    }

    [HttpPost("{id:guid}/aprovar")]
    [ProducesResponseType(typeof(PendenciaUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Aprovar(Guid id, [FromBody] ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken)
    {
        var pendencia = await pendenciaServico.AprovarPartidaAsync(id, dto, cancellationToken);
        return Ok(pendencia);
    }

    [HttpPost("{id:guid}/contestar")]
    [ProducesResponseType(typeof(PendenciaUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Contestar(Guid id, [FromBody] ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken)
    {
        var pendencia = await pendenciaServico.ContestarPartidaAsync(id, dto, cancellationToken);
        return Ok(pendencia);
    }

    [HttpPut("{id:guid}/contato")]
    [ProducesResponseType(typeof(PendenciaUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarContato(
        Guid id,
        [FromBody] AtualizarContatoPendenciaDto dto,
        CancellationToken cancellationToken)
    {
        var pendencia = await pendenciaServico.CompletarContatoAsync(id, dto, cancellationToken);
        return Ok(pendencia);
    }
}
