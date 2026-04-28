using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class CompeticaoRepositorio(PlataformaFutevoleiDbContext dbContext) : ICompeticaoRepositorio
{
    public async Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Competicoes
            .AsNoTracking()
            .Include(x => x.Liga)
            .Include(x => x.Local)
            .Include(x => x.FormatoCampeonato)
            .Include(x => x.RegraCompeticao)
            .Include(x => x.UsuarioOrganizador)
            .OrderByDescending(x => x.DataInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default)
    {
        return await AplicarFiltroAcessoAtleta(
                dbContext.Competicoes.AsNoTracking(),
                usuarioId,
                atletaId)
            .Select(x => x.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AtletaPossuiAcessoAsync(
        Guid competicaoId,
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default)
    {
        return AplicarFiltroAcessoAtleta(
                dbContext.Competicoes.AsNoTracking().Where(x => x.Id == competicaoId),
                usuarioId,
                atletaId)
            .AnyAsync(cancellationToken);
    }

    public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Competicoes
            .Include(x => x.Liga)
            .Include(x => x.Local)
            .Include(x => x.FormatoCampeonato)
            .Include(x => x.RegraCompeticao)
            .Include(x => x.UsuarioOrganizador)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = nome.Trim().ToLowerInvariant();

        return dbContext.Competicoes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Nome.ToLower() == nomeNormalizado, cancellationToken);
    }

    public async Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default)
    {
        await dbContext.Competicoes.AddAsync(competicao, cancellationToken);
    }

    public void Atualizar(Competicao competicao)
    {
        dbContext.Competicoes.Update(competicao);
    }

    public void Remover(Competicao competicao)
    {
        dbContext.Competicoes.Remove(competicao);
    }

    private static IQueryable<Competicao> AplicarFiltroAcessoAtleta(
        IQueryable<Competicao> query,
        Guid usuarioId,
        Guid? atletaId)
    {
        if (!atletaId.HasValue)
        {
            return query.Where(x => x.UsuarioOrganizadorId == usuarioId);
        }

        var atletaIdValor = atletaId.Value;
        return query.Where(x =>
            x.UsuarioOrganizadorId == usuarioId ||
            x.GrupoAtletas.Any(grupo => grupo.AtletaId == atletaIdValor) ||
            x.Inscricoes.Any(inscricao =>
                inscricao.Status != StatusInscricaoCampeonato.Cancelada &&
                (inscricao.Dupla.Atleta1Id == atletaIdValor || inscricao.Dupla.Atleta2Id == atletaIdValor)));
    }
}
