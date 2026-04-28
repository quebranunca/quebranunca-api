using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/atletas")]
public class AtletasController(IAtletaServico atletaServico) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(IReadOnlyList<AtletaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] bool somenteInscritosMinhasCompeticoes = false,
        CancellationToken cancellationToken = default)
    {
        var atletas = await atletaServico.ListarAsync(somenteInscritosMinhasCompeticoes, cancellationToken);
        return Ok(atletas);
    }

    [HttpGet("busca")]
    [ProducesResponseType(typeof(IReadOnlyList<AtletaResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Buscar([FromQuery] string? termo, CancellationToken cancellationToken)
    {
        var atletas = await atletaServico.BuscarAsync(termo, cancellationToken);
        return Ok(atletas);
    }

    [HttpGet("pendencias")]
    [ProducesResponseType(typeof(IReadOnlyList<AtletaPendenciaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPendencias(CancellationToken cancellationToken)
    {
        var pendencias = await atletaServico.ListarPendenciasAsync(cancellationToken);
        return Ok(pendencias);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AtletaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var atleta = await atletaServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(atleta);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(AtletaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObterMeu(CancellationToken cancellationToken)
    {
        var atleta = await atletaServico.ObterMeuAsync(cancellationToken);
        if (atleta is null)
        {
            return NoContent();
        }

        return Ok(atleta);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AtletaDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarAtletaDto dto, CancellationToken cancellationToken)
    {
        var atleta = await atletaServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = atleta.Id }, atleta);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(AtletaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarMeu([FromBody] AtualizarAtletaDto dto, CancellationToken cancellationToken)
    {
        var atleta = await atletaServico.SalvarMeuAsync(dto, cancellationToken);
        return Ok(atleta);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AtletaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarAtletaDto dto, CancellationToken cancellationToken)
    {
        var atleta = await atletaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(atleta);
    }

    [HttpPut("pendencias/{id:guid}/email")]
    [ProducesResponseType(typeof(AtletaPendenciaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InformarEmailPendente(
        Guid id,
        [FromBody] AtualizarEmailAtletaPendenteDto dto,
        CancellationToken cancellationToken)
    {
        var atleta = await atletaServico.InformarEmailPendenteAsync(id, dto, cancellationToken);
        return Ok(atleta);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await atletaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
