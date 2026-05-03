using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class GrupoAtletaRepositorio(PlataformaFutevoleiDbContext dbContext) : IGrupoAtletaRepositorio
{
    public async Task<IReadOnlyList<GrupoAtleta>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GruposAtletas
            .AsNoTracking()
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .Where(x => x.CompeticaoId == competicaoId)
            .OrderBy(x => x.Atleta.Nome)
            .ThenBy(x => x.Atleta.Apelido)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GruposAtletas
            .AsNoTracking()
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .Where(x => x.AtletaId == atletaId)
            .OrderBy(x => x.CompeticaoId)
            .ToListAsync(cancellationToken);
    }

    public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.GruposAtletas
            .Include(x => x.Competicao)
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<GrupoAtleta?> ObterPorCompeticaoEAtletaAsync(Guid competicaoId, Guid atletaId, CancellationToken cancellationToken = default)
    {
        return dbContext.GruposAtletas
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.CompeticaoId == competicaoId && x.AtletaId == atletaId, cancellationToken);
    }

    public async Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default)
    {
        await dbContext.GruposAtletas.AddAsync(grupoAtleta, cancellationToken);
    }

    public void Remover(GrupoAtleta grupoAtleta)
    {
        dbContext.GruposAtletas.Remove(grupoAtleta);
    }
}
