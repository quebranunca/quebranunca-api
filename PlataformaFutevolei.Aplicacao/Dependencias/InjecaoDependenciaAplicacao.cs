using Microsoft.Extensions.DependencyInjection;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;

namespace PlataformaFutevolei.Aplicacao.Dependencias;

public static class InjecaoDependenciaAplicacao
{
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IAutorizacaoUsuarioServico, AutorizacaoUsuarioServico>();
        services.AddScoped<IAutenticacaoServico, AutenticacaoServico>();
        services.AddScoped<IConviteCadastroServico, ConviteCadastroServico>();
        services.AddScoped<ISolicitacaoAcessoServico, SolicitacaoAcessoServico>();
        services.AddScoped<IPrivacidadeServico, PrivacidadeServico>();
        services.AddScoped<IUsuarioServico, UsuarioServico>();
        services.AddScoped<IAdministradorInicialServico, AdministradorInicialServico>();
        services.AddScoped<IAtletaServico, AtletaServico>();
        services.AddScoped<IDashboardAtletaServico, DashboardAtletaServico>();
        services.AddScoped<IDashboardPublicoServico, DashboardPublicoServico>();
        services.AddScoped<IDashboardDuplaServico, DashboardDuplaServico>();
        services.AddScoped<ILigaServico, LigaServico>();
        services.AddScoped<IArenaServico, ArenaServico>();
        services.AddScoped<ILocalServico, LocalServico>();
        services.AddScoped<IFormatoCampeonatoServico, FormatoCampeonatoServico>();
        services.AddScoped<IRegraCompeticaoServico, RegraCompeticaoServico>();
        services.AddScoped<IDuplaServico, DuplaServico>();
        services.AddScoped<ICompeticaoServico, CompeticaoServico>();
        services.AddScoped<IGrupoServico, GrupoServico>();
        services.AddScoped<IGrupoPadraoServico, GrupoPadraoServico>();
        services.AddScoped<IGrupoResumoUsuarioServico, GrupoResumoUsuarioServico>();
        services.AddScoped<IGrupoAtletaServico, GrupoAtletaServico>();
        services.AddScoped<IMassaTesteAiServico, MassaTesteAiServico>();
        services.AddScoped<ICategoriaCompeticaoServico, CategoriaCompeticaoServico>();
        services.AddScoped<IInscricaoCampeonatoServico, InscricaoCampeonatoServico>();
        services.AddScoped<IResolvedorAtletaDuplaServico, ResolvedorAtletaDuplaServico>();
        services.AddScoped<IConsolidacaoAtletaServico, ConsolidacaoAtletaServico>();
        services.AddScoped<IPartidaServico, PartidaServico>();
        services.AddScoped<IPartidaCancelamentoServico, PartidaCancelamentoServico>();
        services.AddScoped<IPendenciaServico, PendenciaServico>();
        services.AddScoped<IPontuacaoBeneficioServico, PontuacaoBeneficioServico>();
        services.AddScoped<IRankingServico, RankingServico>();
        services.AddScoped<IImportacaoServico, ImportacaoServico>();

        return services;
    }
}
