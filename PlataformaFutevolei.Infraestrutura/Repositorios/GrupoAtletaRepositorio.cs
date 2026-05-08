using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class GrupoAtletaRepositorio(PlataformaFutevoleiDbContext dbContext) : IGrupoAtletaRepositorio
{
    public async Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GruposAtletas
            .AsNoTracking()
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .Where(x => x.GrupoId == grupoId)
            .OrderBy(x => x.Atleta.Nome)
            .ThenBy(x => x.Atleta.Apelido)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default)
    {
        var termoNormalizado = termo.Trim().ToLowerInvariant();
        if (termoNormalizado.Length < 3)
        {
            return [];
        }

        return await dbContext.GruposAtletas
            .AsNoTracking()
            .Include(x => x.Atleta)
            .Where(x => x.GrupoId == grupoId)
            .Where(x =>
                x.Atleta.Nome.ToLower().StartsWith(termoNormalizado) ||
                (x.Atleta.Apelido != null && x.Atleta.Apelido.ToLower().StartsWith(termoNormalizado)))
            .OrderBy(x => x.Atleta.Apelido ?? x.Atleta.Nome)
            .ThenBy(x => x.Atleta.Nome)
            .Take(12)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GruposAtletas
            .AsNoTracking()
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .Where(x => x.AtletaId == atletaId)
            .OrderBy(x => x.GrupoId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        return await dbContext.GruposAtletas
            .Where(x => x.AtletaId == atletaId)
            .ToListAsync(cancellationToken);
    }

    public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.GruposAtletas
            .Include(x => x.Grupo)
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default)
    {
        return dbContext.GruposAtletas
            .Include(x => x.Atleta)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.GrupoId == grupoId && x.AtletaId == atletaId, cancellationToken);
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
