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
        services.AddScoped<IUsuarioServico, UsuarioServico>();
        services.AddScoped<IAtletaServico, AtletaServico>();
        services.AddScoped<ILigaServico, LigaServico>();
        services.AddScoped<ILocalServico, LocalServico>();
        services.AddScoped<IFormatoCampeonatoServico, FormatoCampeonatoServico>();
        services.AddScoped<IRegraCompeticaoServico, RegraCompeticaoServico>();
        services.AddScoped<IDuplaServico, DuplaServico>();
        services.AddScoped<ICompeticaoServico, CompeticaoServico>();
        services.AddScoped<IGrupoServico, GrupoServico>();
        services.AddScoped<IGrupoResumoUsuarioServico, GrupoResumoUsuarioServico>();
        services.AddScoped<IGrupoAtletaServico, GrupoAtletaServico>();
        services.AddScoped<ICategoriaCompeticaoServico, CategoriaCompeticaoServico>();
        services.AddScoped<IInscricaoCampeonatoServico, InscricaoCampeonatoServico>();
        services.AddScoped<IResolvedorAtletaDuplaServico, ResolvedorAtletaDuplaServico>();
        services.AddScoped<IPartidaServico, PartidaServico>();
        services.AddScoped<IPendenciaServico, PendenciaServico>();
        services.AddScoped<IRankingServico, RankingServico>();
        services.AddScoped<IImportacaoServico, ImportacaoServico>();

        return services;
    }
}
