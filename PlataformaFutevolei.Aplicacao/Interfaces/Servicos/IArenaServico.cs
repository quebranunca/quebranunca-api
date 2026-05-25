using PlataformaFutevolei.Aplicacao.DTOs;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IArenaServico
{
    Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(
        ArenaFiltroPublicoRequest filtro,
        CancellationToken cancellationToken = default);
    Task<ArenaDetalhePublicoResponse> ObterPublicaPorSlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
    Task<ArenaResumoPublicoResponse> ObterResumoPublicoAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    Task<ArenaAdminDetalheResponse> CriarAdminAsync(
        CriarArenaRequest request,
        CancellationToken cancellationToken = default);
    Task<ArenaAdminDetalheResponse> AtualizarAdminAsync(
        Guid arenaId,
        AtualizarArenaRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArenaAdminResumoResponse>> ListarMinhasAsync(
        CancellationToken cancellationToken = default);
    Task<ArenaAdminDetalheResponse> ObterAdminAsync(
        Guid arenaId,
        CancellationToken cancellationToken = default);
    Task AtualizarStatusAsync(
        Guid arenaId,
        bool ativa,
        CancellationToken cancellationToken = default);
    Task AtualizarVisibilidadeAsync(
        Guid arenaId,
        bool publica,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArenaEspacoAdminResponse>> ListarEspacosAsync(
        Guid arenaId,
        CancellationToken cancellationToken = default);
    Task<ArenaEspacoAdminResponse> CriarEspacoAsync(
        Guid arenaId,
        CriarArenaEspacoRequest request,
        CancellationToken cancellationToken = default);
    Task<ArenaEspacoAdminResponse> AtualizarEspacoAsync(
        Guid arenaId,
        Guid espacoId,
        AtualizarArenaEspacoRequest request,
        CancellationToken cancellationToken = default);
    Task AtualizarStatusEspacoAsync(
        Guid arenaId,
        Guid espacoId,
        bool ativo,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArenaDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<ArenaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ArenaDto> CriarAsync(CriarArenaDto dto, CancellationToken cancellationToken = default);
    Task<ArenaDto> AtualizarAsync(Guid id, AtualizarArenaDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
