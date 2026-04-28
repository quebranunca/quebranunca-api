using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class PartidaAprovacaoRepositorio(PlataformaFutevoleiDbContext dbContext) : IPartidaAprovacaoRepositorio
{
    public async Task<IReadOnlyList<PartidaAprovacao>> ListarPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PartidasAprovacoes
            .Where(x => x.PartidaId == partidaId)
            .OrderBy(x => x.DataSolicitacao)
            .ToListAsync(cancellationToken);
    }

    public Task<PartidaAprovacao?> ObterPorPartidaEAtletaAsync(
        Guid partidaId,
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PartidasAprovacoes
            .FirstOrDefaultAsync(
                x => x.PartidaId == partidaId && x.AtletaId == atletaId,
                cancellationToken);
    }

    public async Task AdicionarAsync(PartidaAprovacao partidaAprovacao, CancellationToken cancellationToken = default)
    {
        await dbContext.PartidasAprovacoes.AddAsync(partidaAprovacao, cancellationToken);
    }

    public void Atualizar(PartidaAprovacao partidaAprovacao)
    {
        dbContext.PartidasAprovacoes.Update(partidaAprovacao);
    }

    public void RemoverIntervalo(IEnumerable<PartidaAprovacao> aprovacoes)
    {
        dbContext.PartidasAprovacoes.RemoveRange(aprovacoes);
    }
}
