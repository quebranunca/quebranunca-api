using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/competicoes/{competicaoId:guid}/grupo-atletas")]
public class GruposAtletasController(IGrupoAtletaServico grupoAtletaServico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GrupoAtletaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(Guid competicaoId, CancellationToken cancellationToken)
    {
        var atletas = await grupoAtletaServico.ListarPorCompeticaoAsync(competicaoId, cancellationToken);
        return Ok(atletas);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GrupoAtletaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(
        Guid competicaoId,
        [FromBody] CriarGrupoAtletaDto dto,
        CancellationToken cancellationToken)
    {
        var atleta = await grupoAtletaServico.CriarAsync(competicaoId, dto, cancellationToken);
        return CreatedAtAction(nameof(Listar), new { competicaoId }, atleta);
    }

    [HttpPut("{id:guid}/email")]
    [ProducesResponseType(typeof(GrupoAtletaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompletarEmail(
        Guid competicaoId,
        Guid id,
        [FromBody] AtualizarEmailGrupoAtletaDto dto,
        CancellationToken cancellationToken)
    {
        var atleta = await grupoAtletaServico.CompletarEmailAsync(competicaoId, id, dto, cancellationToken);
        return Ok(atleta);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid competicaoId, Guid id, CancellationToken cancellationToken)
    {
        await grupoAtletaServico.RemoverAsync(competicaoId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/assumir")]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssumirMeuNome(Guid competicaoId, Guid id, CancellationToken cancellationToken)
    {
        var usuario = await grupoAtletaServico.AssumirMeuNomeNoGrupoAsync(competicaoId, id, cancellationToken);
        return Ok(usuario);
    }
}
