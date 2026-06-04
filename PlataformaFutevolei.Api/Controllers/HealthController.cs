using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlataformaFutevolei.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private const string VersaoPadrao = "1.0.35";
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Obter()
    {
        var versao = _configuration.GetValue<string>("Aplicacao:Version");

        return Ok(new
        {
            version = string.IsNullOrWhiteSpace(versao) ? VersaoPadrao : versao
        });
    }
}
