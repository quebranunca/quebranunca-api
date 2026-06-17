using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/grupos")]
public class GruposController(
    IGrupoServico grupoServico,
    IGrupoResumoUsuarioServico grupoResumoUsuarioServico) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<GrupoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var grupos = await grupoServico.ListarAsync(cancellationToken);
        return Ok(grupos);
    }

    [HttpGet("selecao")]
    [ProducesResponseType(typeof(IReadOnlyList<GrupoSelecaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarParaSelecao(CancellationToken cancellationToken)
    {
        var grupos = await grupoServico.ListarParaSelecaoAsync(cancellationToken);
        return Ok(grupos);
    }

    [HttpGet("verificar-nome")]
    [ProducesResponseType(typeof(GrupoVerificacaoNomeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerificarNome([FromQuery] string nome, CancellationToken cancellationToken)
    {
        var verificacao = await grupoServico.VerificarNomeAsync(nome, cancellationToken);
        return Ok(verificacao);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GrupoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var grupo = await grupoServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(grupo);
    }

    [HttpGet("{id:guid}/dashboard")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GrupoDashboardDetalheDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDashboardGrupo(Guid id, CancellationToken cancellationToken)
    {
        var dashboard = await grupoServico.ObterDashboardAsync(id, cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("resumo-usuario")]
    [ProducesResponseType(typeof(GrupoResumoUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumoUsuario(CancellationToken cancellationToken)
    {
        var resumo = await grupoResumoUsuarioServico.ObterMeuResumoAsync(cancellationToken);
        return Ok(resumo);
    }

    [HttpGet("resumos-usuario")]
    [ProducesResponseType(typeof(IReadOnlyList<GrupoResumoUsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarResumosUsuario(CancellationToken cancellationToken)
    {
        var resumos = await grupoResumoUsuarioServico.ListarMeusResumosAsync(cancellationToken);
        return Ok(resumos);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(GrupoDashboardUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await grupoResumoUsuarioServico.ObterDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GrupoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarGrupoDto dto, CancellationToken cancellationToken)
    {
        var grupo = await grupoServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = grupo.Id }, grupo);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GrupoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarGrupoDto dto, CancellationToken cancellationToken)
    {
        var grupo = await grupoServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(grupo);
    }

    [HttpPost("{id:guid}/imagem")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(GrupoImagemRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarImagem(Guid id, IFormFile arquivo, CancellationToken cancellationToken)
    {
        await using var stream = arquivo?.OpenReadStream();
        var resposta = await grupoServico.AtualizarImagemAsync(
            id,
            new ArquivoFotoPerfilDto(
                stream!,
                arquivo?.FileName ?? string.Empty,
                arquivo?.ContentType,
                arquivo?.Length ?? 0),
            cancellationToken);

        return Ok(resposta);
    }

    [HttpDelete("{id:guid}/imagem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoverImagem(Guid id, CancellationToken cancellationToken)
    {
        await grupoServico.RemoverImagemAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await grupoServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
