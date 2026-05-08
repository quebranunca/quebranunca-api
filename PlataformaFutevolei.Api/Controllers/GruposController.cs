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
    [ProducesResponseType(typeof(IReadOnlyList<GrupoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var grupos = await grupoServico.ListarAsync(cancellationToken);
        return Ok(grupos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GrupoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var grupo = await grupoServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(grupo);
    }

    [HttpGet("resumo-usuario")]
    [ProducesResponseType(typeof(GrupoResumoUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumoUsuario(CancellationToken cancellationToken)
    {
        var resumo = await grupoResumoUsuarioServico.ObterMeuResumoAsync(cancellationToken);
        return Ok(resumo);
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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await grupoServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
