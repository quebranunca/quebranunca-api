using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(PerfilUsuario.Administrador))]
[Route("api/admin/gamificacao")]
public class AdminGamificacaoController(IPontuacaoBeneficioServico pontuacaoBeneficioServico) : ControllerBase
{
    [HttpPost("recalcular-saldo-inicial")]
    [ProducesResponseType(typeof(RecalculoSaldoInicialPontuacaoResultadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecalcularSaldoInicial(
        [FromQuery] bool dryRun = true,
        CancellationToken cancellationToken = default)
    {
        var resultado = await pontuacaoBeneficioServico.RecalcularSaldoInicialRetroativoAsync(dryRun, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("resgates")]
    [ProducesResponseType(typeof(IReadOnlyList<ResgateBeneficioPontuacaoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarResgates(CancellationToken cancellationToken)
    {
        var resgates = await pontuacaoBeneficioServico.ListarResgatesAdministracaoAsync(cancellationToken);
        return Ok(resgates);
    }

    [HttpPost("resgates/{id:guid}/aprovar")]
    [ProducesResponseType(typeof(ResgateBeneficioPontuacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Aprovar(
        Guid id,
        [FromBody] AtualizarStatusResgateBeneficioDto dto,
        CancellationToken cancellationToken)
    {
        var resgate = await pontuacaoBeneficioServico.AprovarResgateAsync(id, dto, cancellationToken);
        return Ok(resgate);
    }

    [HttpPost("resgates/{id:guid}/rejeitar")]
    [ProducesResponseType(typeof(ResgateBeneficioPontuacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rejeitar(
        Guid id,
        [FromBody] AtualizarStatusResgateBeneficioDto dto,
        CancellationToken cancellationToken)
    {
        var resgate = await pontuacaoBeneficioServico.RejeitarResgateAsync(id, dto, cancellationToken);
        return Ok(resgate);
    }

    [HttpPost("resgates/{id:guid}/cancelar")]
    [ProducesResponseType(typeof(ResgateBeneficioPontuacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancelar(
        Guid id,
        [FromBody] AtualizarStatusResgateBeneficioDto dto,
        CancellationToken cancellationToken)
    {
        var resgate = await pontuacaoBeneficioServico.CancelarResgateAsync(id, dto, cancellationToken);
        return Ok(resgate);
    }
}
