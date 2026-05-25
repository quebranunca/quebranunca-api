using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/arenas")]
public class ArenasController(IArenaServico arenaServico) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ArenaListagemPublicaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPublicas(
        [FromQuery] ArenaFiltroPublicoRequest filtro,
        CancellationToken cancellationToken)
    {
        var arenas = await arenaServico.ListarPublicasAsync(filtro, cancellationToken);
        return Ok(arenas);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ArenaDetalhePublicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPublicaPorSlug(string slug, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.ObterPublicaPorSlugAsync(slug, cancellationToken);
        return Ok(arena);
    }

    [HttpGet("{id:guid}/resumo-publico")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ArenaResumoPublicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterResumoPublico(Guid id, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.ObterResumoPublicoAsync(id, cancellationToken);
        return Ok(arena);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ArenaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(arena);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ArenaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarArenaDto dto, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = arena.Id }, arena);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ArenaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarArenaDto dto, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(arena);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await arenaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
