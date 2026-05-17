using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Route("api/admin/solicitacoes-acesso")]
[Authorize(Roles = nameof(PerfilUsuario.Administrador))]
public class AdminSolicitacoesAcessoController(ISolicitacaoAcessoServico solicitacaoAcessoServico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SolicitacaoAcessoAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var solicitacoes = await solicitacaoAcessoServico.ListarAdminAsync(cancellationToken);
        return Ok(solicitacoes);
    }

    [HttpPost("{id:guid}/aprovar")]
    [ProducesResponseType(typeof(SolicitacaoAcessoAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Aprovar(Guid id, CancellationToken cancellationToken)
    {
        var solicitacao = await solicitacaoAcessoServico.AprovarAsync(id, cancellationToken);
        return Ok(solicitacao);
    }

    [HttpPost("{id:guid}/rejeitar")]
    [ProducesResponseType(typeof(SolicitacaoAcessoAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Rejeitar(Guid id, CancellationToken cancellationToken)
    {
        var solicitacao = await solicitacaoAcessoServico.RejeitarAsync(id, cancellationToken);
        return Ok(solicitacao);
    }

    [HttpPost("{id:guid}/enviar-convite")]
    [ProducesResponseType(typeof(SolicitacaoAcessoAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnviarConvite(Guid id, CancellationToken cancellationToken)
    {
        var solicitacao = await solicitacaoAcessoServico.EnviarConviteAsync(id, cancellationToken);
        return Ok(solicitacao);
    }
}
