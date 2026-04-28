using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class DuplaRepositorio(PlataformaFutevoleiDbContext dbContext) : IDuplaRepositorio
{
    public async Task<IReadOnlyList<Dupla>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Duplas
            .AsNoTracking()
            .Include(x => x.Atleta1)
            .Include(x => x.Atleta2)
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Dupla>> ListarInscritasPorOrganizadorAsync(
        Guid usuarioOrganizadorId,
        CancellationToken cancellationToken = default)
    {
        var duplaIds = dbContext.InscricoesCampeonato
            .AsNoTracking()
            .Where(x =>
                x.Status == StatusInscricaoCampeonato.Ativa &&
                x.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId)
            .Select(x => x.DuplaId);

        return await dbContext.Duplas
            .AsNoTracking()
            .Include(x => x.Atleta1)
            .Include(x => x.Atleta2)
            .Where(x => duplaIds.Contains(x.Id))
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> PertenceAoOrganizadorAsync(
        Guid duplaId,
        Guid usuarioOrganizadorId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.InscricoesCampeonato
            .AsNoTracking()
            .AnyAsync(
                x => x.Status == StatusInscricaoCampeonato.Ativa &&
                     x.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId &&
                     x.DuplaId == duplaId,
                cancellationToken);
    }

    public Task<Dupla?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Duplas
            .Include(x => x.Atleta1)
            .Include(x => x.Atleta2)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Dupla>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Duplas
            .AsNoTracking()
            .Include(x => x.Atleta1)
            .Include(x => x.Atleta2)
            .Where(x => x.Atleta1Id == atletaId || x.Atleta2Id == atletaId)
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Dupla?> ObterPorAtletasAsync(Guid atleta1Id, Guid atleta2Id, CancellationToken cancellationToken = default)
    {
        return dbContext.Duplas
            .Include(x => x.Atleta1)
            .Include(x => x.Atleta2)
            .FirstOrDefaultAsync(
                x => (x.Atleta1Id == atleta1Id && x.Atleta2Id == atleta2Id) ||
                     (x.Atleta1Id == atleta2Id && x.Atleta2Id == atleta1Id),
                cancellationToken
            );
    }

    public async Task AdicionarAsync(Dupla dupla, CancellationToken cancellationToken = default)
    {
        await dbContext.Duplas.AddAsync(dupla, cancellationToken);
    }

    public void Atualizar(Dupla dupla)
    {
        dbContext.Duplas.Update(dupla);
    }

    public void Remover(Dupla dupla)
    {
        dbContext.Duplas.Remove(dupla);
    }
}
