using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/partidas")]
public class PartidasController(IPartidaServico partidaServico) : ControllerBase
{
    private const long LimiteUploadMidiaPartidaBytes = 100L * 1024 * 1024;

    [HttpGet]
    [AllowAnonymous]
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

    [HttpGet("registradas-por-mim")]
    [ProducesResponseType(typeof(IReadOnlyList<PartidaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarRegistradasPorMim(CancellationToken cancellationToken)
    {
        var partidas = await partidaServico.ListarRegistradasPorMimAsync(cancellationToken);
        return Ok(partidas);
    }

    [HttpGet("estrutura")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(partida);
    }

    [HttpGet("{id:guid}/compartilhamento")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PartidaCompartilhamentoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterCompartilhamento(Guid id, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.ObterCompartilhamentoAsync(id, cancellationToken);
        return Ok(partida);
    }

    [HttpPost("verificar-duplicidade")]
    [ProducesResponseType(typeof(VerificarDuplicidadePartidaResultadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerificarDuplicidade(
        [FromBody] VerificarDuplicidadePartidaDto dto,
        CancellationToken cancellationToken)
    {
        var resultado = await partidaServico.VerificarDuplicidadeAsync(dto, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarPartidaDto dto, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = partida.Id }, partida);
    }

    [HttpPost("{id:guid}/midia")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteUploadMidiaPartidaBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteUploadMidiaPartidaBytes)]
    [ProducesResponseType(typeof(MidiaPartidaRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarMidia(Guid id, [FromForm] IFormFile arquivo, CancellationToken cancellationToken)
    {
        await using var stream = arquivo is null ? Stream.Null : arquivo.OpenReadStream();
        var resposta = await partidaServico.AtualizarMidiaAsync(
            id,
            new ArquivoMidiaPartidaDto(
                stream,
                arquivo?.FileName ?? string.Empty,
                arquivo?.ContentType,
                arquivo?.Length ?? 0),
            cancellationToken);

        return Ok(resposta);
    }

    [HttpDelete("{id:guid}/midia")]
    [ProducesResponseType(typeof(MidiaPartidaRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoverMidia(Guid id, CancellationToken cancellationToken)
    {
        var resposta = await partidaServico.RemoverMidiaAsync(id, cancellationToken);
        return Ok(resposta);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarPartidaDto dto, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(partida);
    }

    [HttpPut("{id:guid}/edicao-basica")]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarBasica(Guid id, [FromBody] AtualizarPartidaBasicaDto dto, CancellationToken cancellationToken)
    {
        var partida = await partidaServico.AtualizarBasicaAsync(id, dto, cancellationToken);
        return Ok(partida);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await partidaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
