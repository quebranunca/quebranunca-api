using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Infraestrutura.Configuracoes;
using PlataformaFutevolei.Infraestrutura.Persistencia;
using PlataformaFutevolei.Infraestrutura.Repositorios;
using PlataformaFutevolei.Infraestrutura.Seguranca;
using PlataformaFutevolei.Infraestrutura.Servicos;

namespace PlataformaFutevolei.Infraestrutura.Dependencias;

public static class InjecaoDependenciaInfraestrutura
{
    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, IConfiguration configuration)
        => AdicionarInfraestrutura(services, configuration, nomeAmbiente: null);

    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection services,
        IConfiguration configuration,
        string? nomeAmbiente)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var frontendUrlPadrao = ObterPrimeiraUrlConfigurada(configuration["Frontend:Url"]);
        var ambienteCritico = EhAmbienteCritico(nomeAmbiente);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string não configurada. Defina ConnectionStrings:DefaultConnection " +
                "(ou ConnectionStrings__DefaultConnection).");
        }

        if (ambienteCritico && EhConnectionStringLocal(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string de produção inválida. Defina ConnectionStrings__DefaultConnection " +
                "com a connection string do PostgreSQL do ambiente atual.");
        }

        services.AddDbContext<PlataformaFutevoleiDbContext>(options =>
            options.UseNpgsql(ConnectionStringPostgres.Normalizar(connectionString))
        );

        var secaoJwt = configuration.GetSection(ConfiguracaoJwt.Secao);
        var expiracaoMinutos = int.TryParse(secaoJwt["ExpiracaoMinutos"], out var valorExpiracao)
            ? valorExpiracao
            : 21600;
        var expiracaoRefreshTokenDias = int.TryParse(secaoJwt["ExpiracaoRefreshTokenDias"], out var valorExpiracaoRefreshToken)
            ? valorExpiracaoRefreshToken
            : 90;

        var jwt = new ConfiguracaoJwt
        {
            Chave = secaoJwt["Chave"] ?? string.Empty,
            Emissor = secaoJwt["Emissor"] ?? "PlataformaFutevolei",
            Audiencia = secaoJwt["Audiencia"] ?? "PlataformaFutevolei.Web",
            ExpiracaoMinutos = expiracaoMinutos,
            ExpiracaoRefreshTokenDias = expiracaoRefreshTokenDias
        };
        services.Configure<ConfiguracaoJwt>(options =>
        {
            options.Chave = jwt.Chave;
            options.Emissor = jwt.Emissor;
            options.Audiencia = jwt.Audiencia;
            options.ExpiracaoMinutos = jwt.ExpiracaoMinutos;
            options.ExpiracaoRefreshTokenDias = jwt.ExpiracaoRefreshTokenDias;
        });

        var secaoEmailConvites = configuration.GetSection(ConfiguracaoEmailConviteCadastro.Secao);
        services.Configure<ConfiguracaoEmailConviteCadastro>(options =>
        {
            options.BaseUrl = secaoEmailConvites["BaseUrl"] ?? "https://api.resend.com";
            options.ApiKey = ObterValorConfiguracaoOuAmbiente(secaoEmailConvites["ApiKey"], "RESEND_API_KEY")
                ?? string.Empty;
            options.RemetenteEmail = secaoEmailConvites["RemetenteEmail"] ?? string.Empty;
            options.RemetenteNome = secaoEmailConvites["RemetenteNome"];
            options.ReplyTo = secaoEmailConvites["ReplyTo"];
            options.UrlApp = secaoEmailConvites["UrlApp"] ?? frontendUrlPadrao;
        });

        var secaoCodigoLoginDesenvolvimento = configuration.GetSection(ConfiguracaoCodigoLoginDesenvolvimento.Secao);
        services.Configure<ConfiguracaoCodigoLoginDesenvolvimento>(options =>
        {
            options.HabilitarFallbackSemEmail =
                secaoCodigoLoginDesenvolvimento.GetValue<bool>("HabilitarFallbackSemEmail");
        });

        var secaoEmailCodigoLogin = configuration.GetSection(ConfiguracaoEmailCodigoLogin.Secao);
        services.Configure<ConfiguracaoEmailCodigoLogin>(options =>
        {
            options.BaseUrl = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "BaseUrl")
                ?? "https://api.resend.com";
            options.ApiKey = ObterValorConfiguracaoOuAmbiente(
                    ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "ApiKey"),
                    "RESEND_API_KEY")
                ?? string.Empty;
            options.RemetenteEmail = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "RemetenteEmail")
                ?? string.Empty;
            options.RemetenteNome = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "RemetenteNome");
            options.ReplyTo = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "ReplyTo");
            options.UrlApp = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "UrlApp")
                ?? frontendUrlPadrao;
            options.EmailOrigemSobrescrito = secaoEmailCodigoLogin["EmailOrigemSobrescrito"];
            options.EmailDestinoSobrescrito = secaoEmailCodigoLogin["EmailDestinoSobrescrito"];
        });

        var secaoWhatsappConvites = configuration.GetSection(ConfiguracaoWhatsappConviteCadastro.Secao);
        services.Configure<ConfiguracaoWhatsappConviteCadastro>(options =>
        {
            options.Enabled = secaoWhatsappConvites.GetValue<bool>("Enabled");
            options.AccountSid = secaoWhatsappConvites["AccountSid"] ?? string.Empty;
            options.AuthToken = secaoWhatsappConvites["AuthToken"] ?? string.Empty;
            options.RemetenteWhatsapp = secaoWhatsappConvites["RemetenteWhatsapp"] ?? string.Empty;
            options.UrlApp = secaoWhatsappConvites["UrlApp"] ?? frontendUrlPadrao;
        });

        var secaoCloudinary = configuration.GetSection(CloudinaryConfiguracao.Secao);
        services.Configure<CloudinaryConfiguracao>(options =>
        {
            options.CloudName = ObterValorConfiguracaoOuAmbiente(
                    secaoCloudinary["CloudName"],
                    "Cloudinary__CloudName",
                    "CLOUDINARY__CLOUDNAME",
                    "CLOUDINARY_CLOUD_NAME")
                ?? string.Empty;
            options.ApiKey = ObterValorConfiguracaoOuAmbiente(
                    secaoCloudinary["ApiKey"],
                    "Cloudinary__ApiKey",
                    "CLOUDINARY__APIKEY",
                    "CLOUDINARY_API_KEY")
                ?? string.Empty;
            options.ApiSecret = ObterValorConfiguracaoOuAmbiente(
                    secaoCloudinary["ApiSecret"],
                    "Cloudinary__ApiSecret",
                    "CLOUDINARY__APISECRET",
                    "CLOUDINARY_API_SECRET")
                ?? string.Empty;
        });

        services.AddScoped<IUnidadeTrabalho, UnidadeTrabalho>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IUsuarioConsentimentoLgpdRepositorio, UsuarioConsentimentoLgpdRepositorio>();
        services.AddScoped<IConviteCadastroRepositorio, ConviteCadastroRepositorio>();
        services.AddScoped<ISolicitacaoAcessoRepositorio, SolicitacaoAcessoRepositorio>();
        services.AddScoped<IAtletaRepositorio, AtletaRepositorio>();
        services.AddScoped<ILigaRepositorio, LigaRepositorio>();
        services.AddScoped<IArenaRepositorio, ArenaRepositorio>();
        services.AddScoped<IArenaResponsavelRepositorio, ArenaResponsavelRepositorio>();
        services.AddScoped<IFormatoCampeonatoRepositorio, FormatoCampeonatoRepositorio>();
        services.AddScoped<IRegraCompeticaoRepositorio, RegraCompeticaoRepositorio>();
        services.AddScoped<IDuplaRepositorio, DuplaRepositorio>();
        services.AddScoped<ICompeticaoRepositorio, CompeticaoRepositorio>();
        services.AddScoped<IGrupoRepositorio, GrupoRepositorio>();
        services.AddScoped<IGrupoAtletaRepositorio, GrupoAtletaRepositorio>();
        services.AddScoped<ICategoriaCompeticaoRepositorio, CategoriaCompeticaoRepositorio>();
        services.AddScoped<IInscricaoCampeonatoRepositorio, InscricaoCampeonatoRepositorio>();
        services.AddScoped<IPartidaRepositorio, PartidaRepositorio>();
        services.AddScoped<IPartidaAprovacaoRepositorio, PartidaAprovacaoRepositorio>();
        services.AddScoped<IPendenciaUsuarioRepositorio, PendenciaUsuarioRepositorio>();

        services.AddScoped<ISenhaServico, SenhaServicoBcrypt>();
        services.AddScoped<ITokenJwtServico, TokenJwtServico>();
        services.AddScoped<IGeracaoLinkConviteCadastroServico, GeracaoLinkConviteCadastroServico>();
        services.AddScoped<IFotoPerfilService, FotoPerfilService>();
        services.AddScoped<IMidiaPartidaService, MidiaPartidaService>();
        services.AddScoped<ISaneamentoAtletasEmailServico, SaneamentoAtletasEmailServico>();
        services.AddHttpClient<IEnvioEmailCodigoLoginServico, ResendEmailCodigoLoginServico>((serviceProvider, client) =>
        {
            var configuracaoEmail = serviceProvider.GetRequiredService<IOptions<ConfiguracaoEmailCodigoLogin>>().Value;
            client.BaseAddress = CriarUriBaseResendSegura(configuracaoEmail.ObterBaseUrl());
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<IEnvioEmailConviteCadastroServico, ResendEmailConviteCadastroServico>((serviceProvider, client) =>
        {
            var configuracaoEmail = serviceProvider.GetRequiredService<IOptions<ConfiguracaoEmailConviteCadastro>>().Value;
            client.BaseAddress = CriarUriBaseResendSegura(configuracaoEmail.ObterBaseUrl());
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IEnvioWhatsappConviteCadastroServico, TwilioWhatsappConviteCadastroServico>();

        return services;
    }

    private static string? ObterValorConfiguracaoOuAmbiente(string? valorConfigurado, params string[] variaveisAmbiente)
    {
        if (!string.IsNullOrWhiteSpace(valorConfigurado))
        {
            return valorConfigurado;
        }

        foreach (var variavelAmbiente in variaveisAmbiente)
        {
            var valorAmbiente = Environment.GetEnvironmentVariable(variavelAmbiente);
            if (!string.IsNullOrWhiteSpace(valorAmbiente))
            {
                return valorAmbiente;
            }
        }

        return null;
    }

    private static bool EhAmbienteCritico(string? nomeAmbiente)
    {
        var ambiente = nomeAmbiente
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(ambiente, "Production", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(ambiente, "Staging", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EhConnectionStringLocal(string connectionString)
    {
        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(
                ConnectionStringPostgres.Normalizar(connectionString));
            var host = builder.Host?.Trim();

            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string? ObterValorComFallback(
        IConfigurationSection secaoPreferencial,
        IConfigurationSection secaoFallback,
        string chave)
    {
        var valorPreferencial = secaoPreferencial[chave];
        if (!string.IsNullOrWhiteSpace(valorPreferencial))
        {
            return valorPreferencial;
        }

        var valorFallback = secaoFallback[chave];
        return string.IsNullOrWhiteSpace(valorFallback)
            ? null
            : valorFallback;
    }

    private static Uri CriarUriBaseResendSegura(string? baseUrlConfigurada)
    {
        var candidata = $"{(baseUrlConfigurada ?? string.Empty).Trim().TrimEnd('/')}/";
        if (Uri.TryCreate(candidata, UriKind.Absolute, out var uri))
        {
            return uri;
        }

        return new Uri("https://api.resend.com/");
    }

    private static string ObterPrimeiraUrlConfigurada(string? valorConfigurado)
    {
        if (string.IsNullOrWhiteSpace(valorConfigurado))
        {
            return string.Empty;
        }

        return valorConfigurado
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?? string.Empty;
    }
}
