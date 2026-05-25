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
    Task<IReadOnlyList<ArenaDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<ArenaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ArenaDto> CriarAsync(CriarArenaDto dto, CancellationToken cancellationToken = default);
    Task<ArenaDto> AtualizarAsync(Guid id, AtualizarArenaDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
