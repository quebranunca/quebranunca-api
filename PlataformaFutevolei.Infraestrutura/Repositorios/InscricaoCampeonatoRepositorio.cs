using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class InscricaoCampeonatoRepositorio(PlataformaFutevoleiDbContext dbContext) : IInscricaoCampeonatoRepositorio
{
    public async Task<IReadOnlyList<InscricaoCampeonato>> ListarPorCampeonatoAsync(
        Guid campeonatoId,
        Guid? categoriaId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.InscricoesCampeonato
            .AsNoTracking()
            .Include(x => x.Competicao)
            .Include(x => x.CategoriaCompeticao)
            .Include(x => x.Dupla)
                .ThenInclude(x => x.Atleta1)
            .Include(x => x.Dupla)
                .ThenInclude(x => x.Atleta2)
            .Where(x => x.CompeticaoId == campeonatoId);

        if (categoriaId.HasValue)
        {
            query = query.Where(x => x.CategoriaCompeticaoId == categoriaId.Value);
        }

        return await query
            .OrderByDescending(x => x.DataInscricaoUtc)
            .ThenByDescending(x => x.DataCriacao)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarPorCategoriaAsync(
        Guid categoriaId,
        Guid? ignorarInscricaoId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.InscricoesCampeonato
            .AsNoTracking()
            .Where(x => x.CategoriaCompeticaoId == categoriaId);

        if (ignorarInscricaoId.HasValue)
        {
            query = query.Where(x => x.Id != ignorarInscricaoId.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<InscricaoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.InscricoesCampeonato
            .Include(x => x.Competicao)
            .Include(x => x.CategoriaCompeticao)
            .Include(x => x.Dupla)
                .ThenInclude(x => x.Atleta1)
            .Include(x => x.Dupla)
                .ThenInclude(x => x.Atleta2)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<InscricaoCampeonato?> ObterDuplicadaAsync(
        Guid categoriaId,
        Guid duplaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.InscricoesCampeonato
            .FirstOrDefaultAsync(
                x => x.CategoriaCompeticaoId == categoriaId &&
                     x.DuplaId == duplaId,
                cancellationToken);
    }

    public async Task AdicionarAsync(InscricaoCampeonato inscricao, CancellationToken cancellationToken = default)
    {
        await dbContext.InscricoesCampeonato.AddAsync(inscricao, cancellationToken);
    }

    public void Atualizar(InscricaoCampeonato inscricao)
    {
        dbContext.InscricoesCampeonato.Update(inscricao);
    }

    public void Remover(InscricaoCampeonato inscricao)
    {
        dbContext.InscricoesCampeonato.Remove(inscricao);
    }
}
