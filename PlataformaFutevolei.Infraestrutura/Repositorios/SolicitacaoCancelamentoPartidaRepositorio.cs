using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class SolicitacaoCancelamentoPartidaRepositorio(PlataformaFutevoleiDbContext dbContext)
    : ISolicitacaoCancelamentoPartidaRepositorio
{
    public Task<SolicitacaoCancelamentoPartida?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return CriarConsultaDetalhada(usarNoTracking: false)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<SolicitacaoCancelamentoPartida?> ObterPorIdParaAtualizacaoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM solicitacoes_cancelamento_partidas WHERE id = {id} FOR UPDATE",
            cancellationToken);

        return await ObterPorIdAsync(id, cancellationToken);
    }

    public Task<SolicitacaoCancelamentoPartida?> ObterPendentePorPartidaAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default)
    {
        return CriarConsultaDetalhada(usarNoTracking: false)
            .FirstOrDefaultAsync(
                x => x.PartidaId == partidaId &&
                     x.Status == StatusSolicitacaoCancelamentoPartida.Pendente,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SolicitacaoCancelamentoPartida>> ListarPorPartidaAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhada()
            .Where(x => x.PartidaId == partidaId)
            .OrderByDescending(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public Task AdicionarAsync(
        SolicitacaoCancelamentoPartida solicitacao,
        CancellationToken cancellationToken = default)
    {
        return dbContext.SolicitacoesCancelamentoPartidas.AddAsync(solicitacao, cancellationToken).AsTask();
    }

    public void Atualizar(SolicitacaoCancelamentoPartida solicitacao)
    {
        dbContext.SolicitacoesCancelamentoPartidas.Update(solicitacao);
    }

    private IQueryable<SolicitacaoCancelamentoPartida> CriarConsultaDetalhada(bool usarNoTracking = true)
    {
        var consulta = dbContext.SolicitacoesCancelamentoPartidas
            .Include(x => x.Partida)
                .ThenInclude(x => x.DuplaA)
                    .ThenInclude(x => x!.Atleta1)
                        .ThenInclude(x => x.Usuario)
            .Include(x => x.Partida)
                .ThenInclude(x => x.DuplaA)
                    .ThenInclude(x => x!.Atleta2)
                        .ThenInclude(x => x.Usuario)
            .Include(x => x.Partida)
                .ThenInclude(x => x.DuplaB)
                    .ThenInclude(x => x!.Atleta1)
                        .ThenInclude(x => x.Usuario)
            .Include(x => x.Partida)
                .ThenInclude(x => x.DuplaB)
                    .ThenInclude(x => x!.Atleta2)
                        .ThenInclude(x => x.Usuario)
            .Include(x => x.Partida)
                .ThenInclude(x => x.SolicitacoesCancelamento)
            .Include(x => x.SolicitadaPorUsuario)
            .Include(x => x.RespondidaPorUsuario)
            .Include(x => x.DuplaSolicitante)
            .Include(x => x.DuplaAdversaria)
            .Include(x => x.Pendencias)
                .ThenInclude(x => x.Atleta);

        return usarNoTracking ? consulta.AsNoTracking() : consulta;
    }
}
