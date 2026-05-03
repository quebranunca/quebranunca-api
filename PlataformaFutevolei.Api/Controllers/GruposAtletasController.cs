using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/grupos/{grupoId:guid}/atletas")]
public class GruposAtletasController(IGrupoAtletaServico grupoAtletaServico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GrupoAtletaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(Guid grupoId, CancellationToken cancellationToken)
    {
        var atletas = await grupoAtletaServico.ListarPorGrupoAsync(grupoId, cancellationToken);
        return Ok(atletas);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GrupoAtletaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(
        Guid grupoId,
        [FromBody] CriarGrupoAtletaDto dto,
        CancellationToken cancellationToken)
    {
        var atleta = await grupoAtletaServico.CriarAsync(grupoId, dto, cancellationToken);
        return CreatedAtAction(nameof(Listar), new { grupoId }, atleta);
    }

    [HttpPut("{id:guid}/email")]
    [ProducesResponseType(typeof(GrupoAtletaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompletarEmail(
        Guid grupoId,
        Guid id,
        [FromBody] AtualizarEmailGrupoAtletaDto dto,
        CancellationToken cancellationToken)
    {
        var atleta = await grupoAtletaServico.CompletarEmailAsync(grupoId, id, dto, cancellationToken);
        return Ok(atleta);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid grupoId, Guid id, CancellationToken cancellationToken)
    {
        await grupoAtletaServico.RemoverAsync(grupoId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/assumir")]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssumirMeuNome(Guid grupoId, Guid id, CancellationToken cancellationToken)
    {
        var usuario = await grupoAtletaServico.AssumirMeuNomeNoGrupoAsync(grupoId, id, cancellationToken);
        return Ok(usuario);
    }
}
