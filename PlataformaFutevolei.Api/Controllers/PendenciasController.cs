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

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(PendenciasResumoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo(CancellationToken cancellationToken)
    {
        var resumo = await pendenciaServico.ObterResumoAsync(cancellationToken);
        return Ok(resumo);
    }

    [HttpGet("existe")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(AtualizarContatoPendenciaResultadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarContato(
        Guid id,
        [FromBody] AtualizarContatoPendenciaDto dto,
        CancellationToken cancellationToken)
    {
        var resultado = await pendenciaServico.CompletarContatoAsync(id, dto, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{id:guid}/vincular-atleta-cadastrado")]
    [ProducesResponseType(typeof(PendenciaUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmarVinculoAtletaCadastrado(
        Guid id,
        [FromBody] ConfirmarVinculoAtletaPendenciaDto dto,
        CancellationToken cancellationToken)
    {
        var pendencia = await pendenciaServico.ConfirmarVinculoAtletaCadastradoAsync(id, dto, cancellationToken);
        return Ok(pendencia);
    }
}
