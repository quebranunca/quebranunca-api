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

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(UsuarioResumoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterResumo(CancellationToken cancellationToken)
    {
        var resumo = await usuarioServico.ObterMeuResumoAsync(cancellationToken);
        return Ok(resumo);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarMeuUsuario([FromBody] AtualizarMeuUsuarioDto dto, CancellationToken cancellationToken)
    {
        var usuario = await usuarioServico.AtualizarMeuUsuarioAsync(dto, cancellationToken);
        return Ok(usuario);
    }

    [HttpPost("foto-perfil")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FotoPerfilRespostaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AtualizarMinhaFotoPerfil([FromForm] IFormFile arquivo, CancellationToken cancellationToken)
    {
        await using var stream = arquivo?.OpenReadStream();
        var resposta = await usuarioServico.AtualizarMinhaFotoPerfilAsync(
            new ArquivoFotoPerfilDto(
                stream!,
                arquivo?.FileName ?? string.Empty,
                arquivo?.ContentType,
                arquivo?.Length ?? 0),
            cancellationToken);

        return Ok(resposta);
    }

    [HttpPost("me/vincular-atleta")]
    [ProducesResponseType(typeof(UsuarioLogadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VincularMeuAtleta([FromBody] VincularAtletaUsuarioDto dto, CancellationToken cancellationToken)
    {
        var usuario = await usuarioServico.VincularMeuAtletaAsync(dto, cancellationToken);
        return Ok(usuario);
    }

    [HttpDelete("meu-perfil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ExcluirMeuPerfil(CancellationToken cancellationToken)
    {
        await usuarioServico.ExcluirMeuPerfilAsync(cancellationToken);
        return NoContent();
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

    [HttpDelete("~/api/admin/usuarios/{id:guid}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ExcluirPorAdministrador(Guid id, CancellationToken cancellationToken)
    {
        await usuarioServico.ExcluirPorAdministradorAsync(id, cancellationToken);
        return NoContent();
    }
}
