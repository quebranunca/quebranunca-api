using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
[Route("api/campeonatos")]
public class CampeonatosController(ICompeticaoServico competicaoServico) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CampeonatoDetalheDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var campeonato = await competicaoServico.ObterCampeonatoPorIdAsync(id, cancellationToken);
        return Ok(campeonato);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CampeonatoDetalheDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarCampeonatoDto dto, CancellationToken cancellationToken)
    {
        var campeonato = await competicaoServico.CriarCampeonatoAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = campeonato.Campeonato.Id }, campeonato);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CampeonatoDetalheDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarCampeonatoDto dto, CancellationToken cancellationToken)
    {
        var campeonato = await competicaoServico.AtualizarCampeonatoAsync(id, dto, cancellationToken);
        return Ok(campeonato);
    }
}
