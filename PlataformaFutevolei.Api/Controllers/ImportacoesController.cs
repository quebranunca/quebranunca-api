using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(PerfilUsuario.Administrador))]
[Route("api/importacoes")]
public class ImportacoesController(IImportacaoServico importacaoServico) : ControllerBase
{
    [HttpPost("{tipo}")]
    [ProducesResponseType(typeof(ImportacaoResultadoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Importar(
        string tipo,
        [FromForm] IFormFile? arquivo,
        [FromForm] Guid? campeonatoId,
        CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            throw new RegraNegocioException("Selecione um arquivo para importar.");
        }

        await using var stream = arquivo.OpenReadStream();
        var resultado = await importacaoServico.ImportarAsync(
            tipo,
            stream,
            arquivo.FileName,
            campeonatoId,
            cancellationToken);
        return Ok(resultado);
    }
}
