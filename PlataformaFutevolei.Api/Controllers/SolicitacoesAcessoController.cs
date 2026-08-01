using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlataformaFutevolei.Api.Configuracao;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Route("api/solicitacoes-acesso")]
public class SolicitacoesAcessoController(ISolicitacaoAcessoServico solicitacaoAcessoServico) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(ConfiguracaoRateLimiting.PoliticaCadastro)]
    [ProducesResponseType(typeof(SolicitacaoAcessoRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Criar(
        [FromBody] CriarSolicitacaoAcessoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await solicitacaoAcessoServico.CriarAsync(dto, cancellationToken);
        return Ok(resposta);
    }
}
