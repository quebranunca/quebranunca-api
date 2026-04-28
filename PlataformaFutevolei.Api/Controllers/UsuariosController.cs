using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController(IUsuarioServico usuarioServico) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterMeuUsuario(CancellationToken cancellationToken)
    {
        var usuario = await usuarioServico.ObterMeuUsuarioAsync(cancellationToken);
        return Ok(usuario);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarMeuUsuario([FromBody] AtualizarMeuUsuarioDto dto, CancellationToken cancellationToken)
    {
        var usuario = await usuarioServico.AtualizarMeuUsuarioAsync(dto, cancellationToken);
        return Ok(usuario);
    }

    [HttpPost("me/vincular-atleta")]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VincularMeuAtleta([FromBody] VincularAtletaUsuarioDto dto, CancellationToken cancellationToken)
    {
        var usuario = await usuarioServico.VincularMeuAtletaAsync(dto, cancellationToken);
        return Ok(usuario);
    }

    [HttpGet]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? nome, [FromQuery] string? email, CancellationToken cancellationToken)
    {
        var usuarios = await usuarioServico.ListarAsync(nome, email, cancellationToken);
        return Ok(usuarios);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarUsuarioDto dto, CancellationToken cancellationToken)
    {
        var usuario = await usuarioServico.AtualizarAsync(id, dto, cancellationToken);
        return Ok(usuario);
    }
}
