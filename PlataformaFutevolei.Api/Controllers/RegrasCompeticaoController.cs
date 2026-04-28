using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/regras-competicao")]
public class RegrasCompeticaoController(IRegraCompeticaoServico regraServico) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(IReadOnlyList<RegraCompeticaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var regras = await regraServico.ListarAsync(cancellationToken);
        return Ok(regras);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(RegraCompeticaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var regra = await regraServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(regra);
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(RegraCompeticaoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarRegraCompeticaoDto dto, CancellationToken cancellationToken)
    {
        var regra = await regraServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = regra.Id }, regra);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(RegraCompeticaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarRegraCompeticaoDto dto, CancellationToken cancellationToken)
    {
        var regra = await regraServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(regra);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await regraServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
