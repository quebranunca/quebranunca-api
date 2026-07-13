using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class HistoricoPartidaRepositorio(PlataformaFutevoleiDbContext dbContext) : IHistoricoPartidaRepositorio
{
    public async Task<IReadOnlyList<HistoricoPartida>> ListarPorPartidaAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.HistoricosPartidas
            .AsNoTracking()
            .Where(x => x.PartidaIdOriginal == partidaId)
            .OrderByDescending(x => x.DataHoraUtc)
            .ThenByDescending(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public Task AdicionarAsync(HistoricoPartida historico, CancellationToken cancellationToken = default)
    {
        return dbContext.HistoricosPartidas.AddAsync(historico, cancellationToken).AsTask();
    }
}
