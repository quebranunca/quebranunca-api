using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class LigaRepositorio(PlataformaFutevoleiDbContext dbContext) : ILigaRepositorio
{
    public async Task<IReadOnlyList<Liga>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Ligas
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Liga?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Ligas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Liga?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return dbContext.Ligas.FirstOrDefaultAsync(x => x.Nome == nome, cancellationToken);
    }

    public async Task AdicionarAsync(Liga liga, CancellationToken cancellationToken = default)
    {
        await dbContext.Ligas.AddAsync(liga, cancellationToken);
    }

    public void Atualizar(Liga liga)
    {
        dbContext.Ligas.Update(liga);
    }

    public void Remover(Liga liga)
    {
        dbContext.Ligas.Remove(liga);
    }
}
