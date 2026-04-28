using PlataformaFutevolei.Aplicacao.DTOs;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface ILocalServico
{
    Task<IReadOnlyList<LocalDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<LocalDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LocalDto> CriarAsync(CriarLocalDto dto, CancellationToken cancellationToken = default);
    Task<LocalDto> AtualizarAsync(Guid id, AtualizarLocalDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
