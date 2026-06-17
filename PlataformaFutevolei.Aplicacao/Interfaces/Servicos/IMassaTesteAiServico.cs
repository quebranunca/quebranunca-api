using PlataformaFutevolei.Aplicacao.Configuracoes;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IMassaTesteAiServico
{
    Task<MassaTesteAiResultado> GarantirAsync(
        MassaTesteAiConfiguracao configuracao,
        CancellationToken cancellationToken = default);
}

public record MassaTesteAiResultado(
    bool Habilitada,
    bool Executada,
    Guid? UsuarioId,
    Guid? AtletaUsuarioId,
    Guid? GrupoId,
    Guid? ArenaId,
    int AtletasAuxiliares,
    int VinculosGrupo);
