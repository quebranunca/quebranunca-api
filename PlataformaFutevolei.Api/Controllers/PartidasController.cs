using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/partidas")]
public class PartidasController(
    IPartidaServico partidaServico,
    IPartidaCancelamentoServico partidaCancelamentoServico) : ControllerBase
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
        [FromQuery] bool administracao,
        CancellationToken cancellationToken)
    {
        if (administracao)
        {
            var partidasAdministracao = await partidaServico.ListarAdministracaoAsync(cancellationToken);
            return Ok(partidasAdministracao);
        }

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
            var estruturaGrupo = await partidaServico.ListarEstruturaPorGrupoAsync(grupoId.Value, cancellationToken);
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
    [ProducesResponseType(typeof(CriarPartidaResultadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CriarPartidaResultadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar([FromBody] CriarPartidaDto dto, CancellationToken cancellationToken)
    {
        var resultado = await partidaServico.CriarComResultadoAsync(dto, cancellationToken);

        if (resultado.Status is StatusCriacaoPartida.PossivelDuplicidade or StatusCriacaoPartida.RequerConfirmacaoDuplicidade)
        {
            return Ok(resultado);
        }

        if (resultado.Partida is null)
        {
            return Ok(resultado);
        }

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Partida.Id }, resultado);
    }

    [HttpPost("{id:guid}/midia")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteUploadMidiaPartidaBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteUploadMidiaPartidaBytes)]
    [ProducesResponseType(typeof(MidiaPartidaRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarMidia(Guid id, IFormFile arquivo, CancellationToken cancellationToken)
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

    [HttpPost("{id:guid}/solicitacoes-cancelamento")]
    [ProducesResponseType(typeof(SolicitacaoCancelamentoPartidaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SolicitarCancelamento(
        Guid id,
        [FromBody] SolicitarCancelamentoPartidaDto dto,
        CancellationToken cancellationToken)
    {
        var solicitacao = await partidaCancelamentoServico.SolicitarAsync(id, dto, cancellationToken);
        return CreatedAtAction(nameof(ObterSolicitacaoCancelamento), new { id }, solicitacao);
    }

    [HttpGet("{id:guid}/solicitacao-cancelamento")]
    [ProducesResponseType(typeof(SolicitacaoCancelamentoPartidaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterSolicitacaoCancelamento(Guid id, CancellationToken cancellationToken)
    {
        var solicitacao = await partidaCancelamentoServico.ObterAtualAsync(id, cancellationToken);
        return solicitacao is null ? NotFound() : Ok(solicitacao);
    }

    [HttpPost("{id:guid}/solicitacoes-cancelamento/{solicitacaoId:guid}/aprovar")]
    [ProducesResponseType(typeof(SolicitacaoCancelamentoPartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AprovarCancelamento(
        Guid id,
        Guid solicitacaoId,
        CancellationToken cancellationToken)
    {
        var solicitacao = await partidaCancelamentoServico.AprovarAsync(id, solicitacaoId, cancellationToken);
        return Ok(solicitacao);
    }

    [HttpPost("{id:guid}/solicitacoes-cancelamento/{solicitacaoId:guid}/recusar")]
    [ProducesResponseType(typeof(SolicitacaoCancelamentoPartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecusarCancelamento(
        Guid id,
        Guid solicitacaoId,
        CancellationToken cancellationToken)
    {
        var solicitacao = await partidaCancelamentoServico.RecusarAsync(id, solicitacaoId, cancellationToken);
        return Ok(solicitacao);
    }

    [HttpPost("{id:guid}/solicitacoes-cancelamento/{solicitacaoId:guid}/cancelar")]
    [ProducesResponseType(typeof(SolicitacaoCancelamentoPartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelarSolicitacaoCancelamento(
        Guid id,
        Guid solicitacaoId,
        CancellationToken cancellationToken)
    {
        var solicitacao = await partidaCancelamentoServico.CancelarSolicitacaoAsync(id, solicitacaoId, cancellationToken);
        return Ok(solicitacao);
    }

    [HttpPost("{id:guid}/cancelar")]
    [ProducesResponseType(typeof(PartidaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancelar(
        Guid id,
        [FromBody] CancelarPartidaDto dto,
        CancellationToken cancellationToken)
    {
        var partida = await partidaCancelamentoServico.CancelarDiretamenteAsync(id, dto, cancellationToken);
        return Ok(partida);
    }

    [HttpPost("{id:guid}/excluir-definitivamente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ExcluirDefinitivamente(
        Guid id,
        [FromBody] ExcluirPartidaDefinitivamenteDto dto,
        CancellationToken cancellationToken)
    {
        await partidaCancelamentoServico.ExcluirDefinitivamenteAsync(id, dto, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(
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
