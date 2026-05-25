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

    [HttpPost("admin")]
    [ProducesResponseType(typeof(ArenaAdminDetalheResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarAdmin([FromBody] CriarArenaRequest request, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.CriarAdminAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterAdmin), new { arenaId = arena.Id }, arena);
    }

    [HttpGet("admin/minhas")]
    [ProducesResponseType(typeof(IReadOnlyList<ArenaAdminResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMinhas(CancellationToken cancellationToken)
    {
        var arenas = await arenaServico.ListarMinhasAsync(cancellationToken);
        return Ok(arenas);
    }

    [HttpGet("admin/{arenaId:guid}")]
    [ProducesResponseType(typeof(ArenaAdminDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterAdmin(Guid arenaId, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.ObterAdminAsync(arenaId, cancellationToken);
        return Ok(arena);
    }

    [HttpPut("admin/{arenaId:guid}")]
    [ProducesResponseType(typeof(ArenaAdminDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarAdmin(
        Guid arenaId,
        [FromBody] AtualizarArenaRequest request,
        CancellationToken cancellationToken)
    {
        var arena = await arenaServico.AtualizarAdminAsync(arenaId, request, cancellationToken);
        return Ok(arena);
    }

    [HttpPatch("admin/{arenaId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarStatus(
        Guid arenaId,
        [FromBody] AtualizarStatusArenaRequest request,
        CancellationToken cancellationToken)
    {
        await arenaServico.AtualizarStatusAsync(arenaId, request.Ativa, cancellationToken);
        return NoContent();
    }

    [HttpPatch("admin/{arenaId:guid}/visibilidade")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarVisibilidade(
        Guid arenaId,
        [FromBody] AtualizarVisibilidadeArenaRequest request,
        CancellationToken cancellationToken)
    {
        await arenaServico.AtualizarVisibilidadeAsync(arenaId, request.Publica, cancellationToken);
        return NoContent();
    }

    [HttpGet("admin/{arenaId:guid}/espacos")]
    [ProducesResponseType(typeof(IReadOnlyList<ArenaEspacoAdminResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarEspacos(Guid arenaId, CancellationToken cancellationToken)
    {
        var espacos = await arenaServico.ListarEspacosAsync(arenaId, cancellationToken);
        return Ok(espacos);
    }

    [HttpPost("admin/{arenaId:guid}/espacos")]
    [ProducesResponseType(typeof(ArenaEspacoAdminResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CriarEspaco(
        Guid arenaId,
        [FromBody] CriarArenaEspacoRequest request,
        CancellationToken cancellationToken)
    {
        var espaco = await arenaServico.CriarEspacoAsync(arenaId, request, cancellationToken);
        return CreatedAtAction(nameof(ListarEspacos), new { arenaId }, espaco);
    }

    [HttpPut("admin/{arenaId:guid}/espacos/{espacoId:guid}")]
    [ProducesResponseType(typeof(ArenaEspacoAdminResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarEspaco(
        Guid arenaId,
        Guid espacoId,
        [FromBody] AtualizarArenaEspacoRequest request,
        CancellationToken cancellationToken)
    {
        var espaco = await arenaServico.AtualizarEspacoAsync(arenaId, espacoId, request, cancellationToken);
        return Ok(espaco);
    }

    [HttpPatch("admin/{arenaId:guid}/espacos/{espacoId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarStatusEspaco(
        Guid arenaId,
        Guid espacoId,
        [FromBody] AtualizarStatusArenaEspacoRequest request,
        CancellationToken cancellationToken)
    {
        await arenaServico.AtualizarStatusEspacoAsync(arenaId, espacoId, request.Ativo, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ArenaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarArenaDto dto, CancellationToken cancellationToken)
    {
        var arena = await arenaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(arena);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await arenaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }

}
