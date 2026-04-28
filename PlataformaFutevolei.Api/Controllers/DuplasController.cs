using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/duplas")]
public class DuplasController(IDuplaServico duplaServico) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(IReadOnlyList<DuplaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] bool somenteInscritasMinhasCompeticoes = false,
        CancellationToken cancellationToken = default)
    {
        var duplas = await duplaServico.ListarAsync(somenteInscritasMinhasCompeticoes, cancellationToken);
        return Ok(duplas);
    }

    [HttpGet("por-atleta/{id:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<DuplaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPorAtleta(Guid id, CancellationToken cancellationToken)
    {
        var duplas = await duplaServico.ListarPorAtletaAsync(id, cancellationToken);
        return Ok(duplas);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(DuplaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var dupla = await duplaServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(dupla);
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(DuplaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarDuplaDto dto, CancellationToken cancellationToken)
    {
        var dupla = await duplaServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = dupla.Id }, dupla);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(DuplaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarDuplaDto dto, CancellationToken cancellationToken)
    {
        var dupla = await duplaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(dupla);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await duplaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
