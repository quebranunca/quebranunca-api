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
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("Padrao");
        var frontendUrlPadrao = ObterPrimeiraUrlConfigurada(configuration["Frontend:Url"]);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string não configurada. Defina ConnectionStrings:DefaultConnection " +
                "(ou ConnectionStrings:Padrao / ConnectionStrings__DefaultConnection).");
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
            options.ApiKey = secaoEmailConvites["ApiKey"] ?? string.Empty;
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
            options.ApiKey = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "ApiKey")
                ?? string.Empty;
            options.RemetenteEmail = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "RemetenteEmail")
                ?? string.Empty;
            options.RemetenteNome = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "RemetenteNome");
            options.ReplyTo = ObterValorComFallback(secaoEmailCodigoLogin, secaoEmailConvites, "ReplyTo");
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

        services.AddScoped<IUnidadeTrabalho, UnidadeTrabalho>();
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IConviteCadastroRepositorio, ConviteCadastroRepositorio>();
        services.AddScoped<IAtletaRepositorio, AtletaRepositorio>();
        services.AddScoped<ILigaRepositorio, LigaRepositorio>();
        services.AddScoped<ILocalRepositorio, LocalRepositorio>();
        services.AddScoped<IFormatoCampeonatoRepositorio, FormatoCampeonatoRepositorio>();
        services.AddScoped<IRegraCompeticaoRepositorio, RegraCompeticaoRepositorio>();
        services.AddScoped<IDuplaRepositorio, DuplaRepositorio>();
        services.AddScoped<ICompeticaoRepositorio, CompeticaoRepositorio>();
        services.AddScoped<IGrupoAtletaRepositorio, GrupoAtletaRepositorio>();
        services.AddScoped<ICategoriaCompeticaoRepositorio, CategoriaCompeticaoRepositorio>();
        services.AddScoped<IInscricaoCampeonatoRepositorio, InscricaoCampeonatoRepositorio>();
        services.AddScoped<IPartidaRepositorio, PartidaRepositorio>();
        services.AddScoped<IPartidaAprovacaoRepositorio, PartidaAprovacaoRepositorio>();
        services.AddScoped<IPendenciaUsuarioRepositorio, PendenciaUsuarioRepositorio>();

        services.AddScoped<ISenhaServico, SenhaServicoBcrypt>();
        services.AddScoped<ITokenJwtServico, TokenJwtServico>();
        services.AddScoped<IGeracaoLinkConviteCadastroServico, GeracaoLinkConviteCadastroServico>();
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
