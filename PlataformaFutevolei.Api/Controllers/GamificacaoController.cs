using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gamificacao")]
public class GamificacaoController(IPontuacaoBeneficioServico pontuacaoBeneficioServico) : ControllerBase
{
    [HttpGet("resumo")]
    [ProducesResponseType(typeof(GamificacaoResumoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo(CancellationToken cancellationToken)
    {
        var resumo = await pontuacaoBeneficioServico.ObterResumoAsync(cancellationToken);
        return Ok(resumo);
    }

    [HttpGet("extrato")]
    [ProducesResponseType(typeof(ExtratoPontuacaoBeneficioListaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarExtrato(
        [FromQuery] TipoEventoPontuacaoBeneficio? tipo,
        [FromQuery] DateTime? dataInicial,
        [FromQuery] DateTime? dataFinal,
        [FromQuery] int pagina = 1,
        [FromQuery] int quantidadePorPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var extrato = await pontuacaoBeneficioServico.ListarExtratoAsync(
            tipo,
            dataInicial,
            dataFinal,
            pagina,
            quantidadePorPagina,
            cancellationToken);
        return Ok(extrato);
    }

    [HttpGet("beneficios")]
    [ProducesResponseType(typeof(IReadOnlyList<BeneficioPontuacaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarBeneficios(
        [FromQuery] TipoBeneficioPontuacao? tipo,
        [FromQuery] bool? disponivel,
        [FromQuery] bool? destaque,
        CancellationToken cancellationToken)
    {
        var beneficios = await pontuacaoBeneficioServico.ListarBeneficiosAsync(
            tipo,
            disponivel,
            destaque,
            cancellationToken);
        return Ok(beneficios);
    }

    [HttpPost("beneficios/{beneficioId:guid}/resgatar")]
    [ProducesResponseType(typeof(ResgateBeneficioPontuacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SolicitarResgate(
        Guid beneficioId,
        [FromBody] SolicitarResgateBeneficioDto dto,
        CancellationToken cancellationToken)
    {
        var resgate = await pontuacaoBeneficioServico.SolicitarResgateAsync(beneficioId, dto, cancellationToken);
        return Ok(resgate);
    }

    [HttpGet("resgates")]
    [ProducesResponseType(typeof(IReadOnlyList<ResgateBeneficioPontuacaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarResgates(CancellationToken cancellationToken)
    {
        var resgates = await pontuacaoBeneficioServico.ListarMeusResgatesAsync(cancellationToken);
        return Ok(resgates);
    }

    [HttpGet("missoes")]
    [ProducesResponseType(typeof(IReadOnlyList<MissaoPontuacaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarMissoes(CancellationToken cancellationToken)
    {
        var missoes = await pontuacaoBeneficioServico.ListarMissoesAsync(cancellationToken);
        return Ok(missoes);
    }

    [HttpGet("conquistas")]
    [ProducesResponseType(typeof(IReadOnlyList<ConquistaAtletaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarConquistas(CancellationToken cancellationToken)
    {
        var conquistas = await pontuacaoBeneficioServico.ListarConquistasAsync(cancellationToken);
        return Ok(conquistas);
    }

    [HttpPost("compartilhamentos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RegistrarCompartilhamento(
        [FromBody] RegistrarCompartilhamentoGamificacaoDto dto,
        CancellationToken cancellationToken)
    {
        await pontuacaoBeneficioServico.RegistrarCompartilhamentoAsync(dto, cancellationToken);
        return NoContent();
    }
}
