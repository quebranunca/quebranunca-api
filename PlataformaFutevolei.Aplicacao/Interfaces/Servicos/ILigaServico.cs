using PlataformaFutevolei.Aplicacao.DTOs;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface ILigaServico
{
    Task<IReadOnlyList<LigaDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<LigaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LigaDto> CriarAsync(CriarLigaDto dto, CancellationToken cancellationToken = default);
    Task<LigaDto> AtualizarAsync(Guid id, AtualizarLigaDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
