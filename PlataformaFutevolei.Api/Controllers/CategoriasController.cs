using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categorias")]
public class CategoriasController(ICategoriaCompeticaoServico categoriaServico, IPartidaServico partidaServico) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoriaCompeticaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
    {
        var categoria = await categoriaServico.ObterPorIdAsync(id, cancellationToken);
        return Ok(categoria);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoriaCompeticaoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarCategoriaCompeticaoDto dto, CancellationToken cancellationToken)
    {
        var categoria = await categoriaServico.CriarAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = categoria.Id }, categoria);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoriaCompeticaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarCategoriaCompeticaoDto dto, CancellationToken cancellationToken)
    {
        var categoria = await categoriaServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(categoria);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await categoriaServico.RemoverAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/partidas/aprovar")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(CategoriaCompeticaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AprovarTabelaPartidas(Guid id, CancellationToken cancellationToken)
    {
        var categoria = await categoriaServico.AprovarTabelaJogosAsync(id, cancellationToken);
        return Ok(categoria);
    }

    [HttpGet("{id:guid}/partidas")]
    [ProducesResponseType(typeof(IReadOnlyList<PartidaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPartidas(Guid id, CancellationToken cancellationToken)
    {
        var partidas = await partidaServico.ListarPorCategoriaAsync(id, cancellationToken);
        return Ok(partidas);
    }

    [HttpGet("{id:guid}/estrutura")]
    [ProducesResponseType(typeof(IReadOnlyList<RodadaEstruturaCompeticaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarEstrutura(Guid id, CancellationToken cancellationToken)
    {
        var estrutura = await partidaServico.ListarEstruturaPorCategoriaAsync(id, cancellationToken);
        return Ok(estrutura);
    }

    [HttpGet("{id:guid}/chaveamento")]
    [ProducesResponseType(typeof(ChaveamentoCategoriaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterChaveamento(Guid id, CancellationToken cancellationToken)
    {
        var chaveamento = await partidaServico.ObterChaveamentoPorCategoriaAsync(id, cancellationToken);
        return Ok(chaveamento);
    }

    [HttpGet("{id:guid}/duplas/situacao")]
    [ProducesResponseType(typeof(IReadOnlyList<SituacaoDuplaCompeticaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarSituacaoDuplas(Guid id, CancellationToken cancellationToken)
    {
        var situacoes = await partidaServico.ListarSituacaoDuplasPorCategoriaAsync(id, cancellationToken);
        return Ok(situacoes);
    }

    [HttpPost("{id:guid}/partidas/gerar-tabela")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(GeracaoTabelaCategoriaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GerarTabelaPartidas(
        Guid id,
        [FromBody] GerarTabelaCategoriaDto dto,
        CancellationToken cancellationToken)
    {
        var resultado = await partidaServico.GerarTabelaCategoriaAsync(id, dto, cancellationToken);
        return Ok(resultado);
    }

    [HttpDelete("{id:guid}/partidas")]
    [Authorize(Roles = $"{nameof(PerfilUsuario.Administrador)},{nameof(PerfilUsuario.Organizador)}")]
    [ProducesResponseType(typeof(RemocaoTabelaCategoriaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoverTabelaPartidas(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await partidaServico.RemoverTabelaCategoriaAsync(id, cancellationToken);
        return Ok(resultado);
    }
}
