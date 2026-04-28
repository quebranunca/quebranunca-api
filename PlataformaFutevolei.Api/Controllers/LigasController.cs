using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ligas")]
public class LigasController(ILigaServico ligaServico) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<LigaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var ligas = await ligaServico.ListarAsync(cancellationToken);
        return Ok(ligas);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LigaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var liga = await ligaServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(liga);
    }

    [HttpPost]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(typeof(LigaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarLigaDto dto, CancellationToken cancellationToken)
    {
        var liga = await ligaServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = liga.Id }, liga);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(typeof(LigaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarLigaDto dto, CancellationToken cancellationToken)
    {
        var liga = await ligaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(liga);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await ligaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
