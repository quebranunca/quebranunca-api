using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/partidas")]
public class PartidasController(IPartidaServico partidaServico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PartidaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? competicaoId,
        [FromQuery] Guid? grupoId,
        [FromQuery] Guid? categoriaId,
        [FromQuery] bool minhas,
        CancellationToken cancellationToken)
    {
        if (minhas)
        {
            var minhasPartidas = await partidaServico.ListarMinhasAsync(cancellationToken);
            return Ok(minhasPartidas);
        }

        if (categoriaId.HasValue)
        {
            var partidasCategoria = await partidaServico.ListarPorCategoriaAsync(categoriaId.Value, cancellationToken);
            return Ok(partidasCategoria);
        }

        if (grupoId.HasValue)
        {
            var partidasGrupo = await partidaServico.ListarPorGrupoAsync(grupoId.Value, cancellationToken);
            return Ok(partidasGrupo);
        }

        if (competicaoId.HasValue)
        {
            var partidasCompeticao = await partidaServico.ListarPorCompeticaoAsync(competicaoId.Value, cancellationToken);
            return Ok(partidasCompeticao);
        }

        return BadRequest("Informe uma competição ou categoria para consultar as partidas.");
    }

    [HttpGet("minhas")]
    [ProducesResponseType(typeof(IReadOnlyList<PartidaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMinhas(CancellationToken cancellationToken)
    {
        var partidas = await partidaServico.ListarMinhasAsync(cancellationToken);
        return Ok(partidas);
    }

    [HttpGet("estrutura")]
    [ProducesResponseType(typeof(IReadOnlyList<RodadaEstruturaCompeticaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarEstrutura(
        [FromQuery] Guid? competicaoId,
        [FromQuery] Guid? grupoId,
        [FromQuery] Guid? categoriaId,
        CancellationToken cancellationToken)
    {
        if (categoriaId.HasValue)
        {
            var estruturaCategoria = await partidaServico.ListarEstruturaPorCategoriaAsync(categoriaId.Value, cancellationToken);
            return Ok(estruturaCategoria);
        }

        if (grupoId.HasValue)
        {
            var estruturaGrupo = await partidaServico.ListarEstruturaPorCompeticaoAsync(grupoId.Value, cancellationToken);
            return Ok(estruturaGrupo);
        }

        if (competicaoId.HasValue)
        {
            var estruturaCompeticao = await partidaServico.ListarEstruturaPorCompeticaoAsync(competicaoId.Value, cancellationToken);
            return Ok(estruturaCompeticao);
        }

        return BadRequest("Informe uma competição ou categoria para consultar a estrutura das partidas.");
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(partida);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarPartidaDto dto, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = partida.Id }, partida);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarPartidaDto dto, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(partida);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await partidaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
