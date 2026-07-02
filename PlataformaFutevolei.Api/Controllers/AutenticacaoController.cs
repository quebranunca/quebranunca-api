using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public class AutenticacaoController(IAutenticacaoServico autenticacaoServico) : ControllerBase
{
    [HttpPost("registrar")]
    [HttpPost("registrar-por-convite")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespostaAutenticacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegistrarPorConvite([FromBody] RegistrarUsuarioRequisicaoDto dto, CancellationToken cancellationToken)
    {
        var dtoComAuditoria = dto with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        var resposta = await autenticacaoServico.RegistrarAsync(dtoComAuditoria, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("iniciar-acesso")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IniciarAcessoRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> IniciarAcesso(
        [FromBody] IniciarAcessoRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.IniciarAcessoAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("confirmar-codigo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ConfirmarCodigoAcessoRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmarCodigo(
        [FromBody] ConfirmarCodigoAcessoRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.ConfirmarCodigoAcessoAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("completar-cadastro-publico")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespostaAutenticacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompletarCadastroPublico(
        [FromBody] CompletarCadastroPublicoRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var dtoComAuditoria = dto with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };
        var resposta = await autenticacaoServico.CompletarCadastroPublicoAsync(dtoComAuditoria, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespostaAutenticacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequisicaoDto dto, CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.LoginAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("login/codigo/solicitar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SolicitarCodigoLoginRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SolicitarCodigoLogin(
        [FromBody] SolicitarCodigoLoginRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.SolicitarCodigoLoginAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("login/codigo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespostaAutenticacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> LoginComCodigo(
        [FromBody] LoginCodigoRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.LoginComCodigoAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("renovar-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespostaAutenticacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RenovarToken(
        [FromBody] RenovarTokenRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.RenovarTokenAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("esqueci-senha/solicitar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SolicitarRedefinicaoSenhaRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SolicitarRedefinicaoSenha(
        [FromBody] EsqueciSenhaRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.SolicitarRedefinicaoSenhaAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("esqueci-senha/redefinir")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RedefinirSenha(
        [FromBody] RedefinirSenhaRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        await autenticacaoServico.RedefinirSenhaAsync(dto, cancellationToken);
        return Ok(new { mensagem = "Senha redefinida com sucesso." });
    }

    [HttpGet("seguranca")]
    [Authorize]
    [ProducesResponseType(typeof(SegurancaUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterSeguranca(CancellationToken cancellationToken)
    {
        var seguranca = await autenticacaoServico.ObterSegurancaUsuarioAtualAsync(cancellationToken);
        return Ok(seguranca);
    }

    [HttpPost("definir-senha")]
    [HttpPost("criar-senha")]
    [Authorize]
    [ProducesResponseType(typeof(SegurancaUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DefinirSenha(
        [FromBody] DefinirSenhaRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var seguranca = await autenticacaoServico.DefinirSenhaAsync(dto, cancellationToken);
        return Ok(seguranca);
    }

    [HttpPost("criar-senha-com-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RespostaAutenticacaoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CriarSenhaComToken(
        [FromBody] CriarSenhaComTokenRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticacaoServico.CriarSenhaComTokenAsync(dto, cancellationToken);
        return Ok(resposta);
    }

    [HttpPost("alterar-senha")]
    [Authorize]
    [ProducesResponseType(typeof(SegurancaUsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AlterarSenha(
        [FromBody] AlterarSenhaRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var seguranca = await autenticacaoServico.AlterarSenhaAsync(dto, cancellationToken);
        return Ok(seguranca);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var usuario = await autenticacaoServico.ObterUsuarioAtualAsync(cancellationToken);
        return Ok(usuario);
    }
}
