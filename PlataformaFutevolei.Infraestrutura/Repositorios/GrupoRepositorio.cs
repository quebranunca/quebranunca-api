using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class GrupoRepositorio(PlataformaFutevoleiDbContext dbContext) : IGrupoRepositorio
{
    private const string NomeGrupoPartidasAvulsas = "Partidas avulsas";

    public async Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Grupos
            .AsNoTracking()
            .Include(x => x.Local)
            .Include(x => x.UsuarioOrganizador)
            .OrderByDescending(x => x.DataInicio)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Grupo?> ObterResumoUsuarioAsync(
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default)
    {
        var consulta = AplicarFiltroAcessoAtleta(
                dbContext.Grupos
                    .AsNoTracking()
                    .Where(x => x.Nome.ToLower() != NomeGrupoPartidasAvulsas.ToLower()),
                usuarioId,
                atletaId);

        return consulta
            .OrderByDescending(x => x.UsuarioOrganizadorId == usuarioId)
            .ThenByDescending(x => x.DataAtualizacao)
            .ThenByDescending(x => x.DataInicio)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default)
    {
        return await AplicarFiltroAcessoAtleta(
                dbContext.Grupos.AsNoTracking(),
                usuarioId,
                atletaId)
            .Select(x => x.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AtletaPossuiAcessoAsync(
        Guid grupoId,
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default)
    {
        return AplicarFiltroAcessoAtleta(
                dbContext.Grupos.AsNoTracking().Where(x => x.Id == grupoId),
                usuarioId,
                atletaId)
            .AnyAsync(cancellationToken);
    }

    public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Grupos
            .Include(x => x.Local)
            .Include(x => x.UsuarioOrganizador)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Grupo?> ObterPorNomeEOrganizadorAsync(
        string nome,
        Guid? usuarioOrganizadorId,
        CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = nome.Trim().ToLowerInvariant();
        return dbContext.Grupos
            .FirstOrDefaultAsync(
                x => x.Nome.Trim().ToLower() == nomeNormalizado &&
                     x.UsuarioOrganizadorId == usuarioOrganizadorId,
                cancellationToken);
    }


    public Task<Grupo?> ObterPorNomeNormalizadoAsync(
        string nome,
        CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = nome.Trim().ToLowerInvariant();
        return dbContext.Grupos
            .Include(x => x.Local)
            .Include(x => x.UsuarioOrganizador)
            .FirstOrDefaultAsync(x => x.Nome.Trim().ToLower() == nomeNormalizado, cancellationToken);
    }

    public async Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default)
    {
        await dbContext.Grupos.AddAsync(grupo, cancellationToken);
    }

    public void Atualizar(Grupo grupo)
    {
        dbContext.Grupos.Update(grupo);
    }

    public void Remover(Grupo grupo)
    {
        dbContext.Grupos.Remove(grupo);
    }

    private static IQueryable<Grupo> AplicarFiltroAcessoAtleta(
        IQueryable<Grupo> query,
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
            x.Nome.Trim().ToLower() == GruposPadrao.NomeGeral.ToLower() ||
            x.Atletas.Any(grupo => grupo.AtletaId == atletaIdValor) ||
            x.Partidas.Any(partida =>
                partida.DuplaA != null &&
                partida.DuplaB != null &&
                (partida.DuplaA.Atleta1Id == atletaIdValor ||
                 partida.DuplaA.Atleta2Id == atletaIdValor ||
                 partida.DuplaB.Atleta1Id == atletaIdValor ||
                 partida.DuplaB.Atleta2Id == atletaIdValor)));
    }
}
