using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class LocalRepositorio(PlataformaFutevoleiDbContext dbContext) : ILocalRepositorio
{
    public async Task<IReadOnlyList<Local>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Locais
            .AsNoTracking()
            .Include(x => x.UsuarioCriador)
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Local?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Locais
            .Include(x => x.UsuarioCriador)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Local?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return dbContext.Locais.FirstOrDefaultAsync(x => x.Nome == nome, cancellationToken);
    }

    public async Task AdicionarAsync(Local local, CancellationToken cancellationToken = default)
    {
        await dbContext.Locais.AddAsync(local, cancellationToken);
    }

    public void Atualizar(Local local)
    {
        dbContext.Locais.Update(local);
    }

    public void Remover(Local local)
    {
        dbContext.Locais.Remove(local);
    }
}
