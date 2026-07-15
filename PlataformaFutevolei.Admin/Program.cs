using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.Dependencias;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Infraestrutura.Dependencias;

const string ComandoPromoverAdminInicial = "promover-admin-inicial";

var comando = args.FirstOrDefault();
if (!string.Equals(comando, ComandoPromoverAdminInicial, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Comando inválido. Use: dotnet run --project PlataformaFutevolei.Admin -- {ComandoPromoverAdminInicial}");
    return 2;
}

var raizRepositorio = Directory.GetCurrentDirectory();
var diretorioApi = Path.Combine(raizRepositorio, "PlataformaFutevolei.Api");
if (!Directory.Exists(diretorioApi))
{
    diretorioApi = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PlataformaFutevolei.Api"));
}

var ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(diretorioApi)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{ambiente}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
}));
services.AdicionarAplicacao();
services.AdicionarInfraestrutura(configuration, ambiente);

await using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdministradorInicial");
var configuracaoAdministracao = configuration.GetSection(AdministracaoConfiguracao.Secao).Get<AdministracaoConfiguracao>()
    ?? new AdministracaoConfiguracao();

var emailAdministradorInicial = configuracaoAdministracao.EmailAdministradorInicial;
if (string.IsNullOrWhiteSpace(emailAdministradorInicial))
{
    logger.LogError("Administracao:EmailAdministradorInicial não configurado.");
    return 1;
}

using var scope = serviceProvider.CreateScope();
var servico = scope.ServiceProvider.GetRequiredService<IAdministradorInicialServico>();

try
{
    var resultado = await servico.PromoverAsync(emailAdministradorInicial);
    logger.LogInformation(
        resultado.Promovido
            ? "Administrador inicial promovido com sucesso. UsuarioId={UsuarioId}; Email={Email}."
            : "Administrador inicial já estava configurado. UsuarioId={UsuarioId}; Email={Email}.",
        resultado.UsuarioId,
        resultado.Email);

    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Falha ao promover administrador inicial.");
    return 1;
}
