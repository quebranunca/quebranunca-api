using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class RegraCompeticaoRepositorio(PlataformaFutevoleiDbContext dbContext) : IRegraCompeticaoRepositorio
{
    public async Task<IReadOnlyList<RegraCompeticao>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RegrasCompeticao
            .AsNoTracking()
            .Include(x => x.UsuarioCriador)
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<RegraCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.RegrasCompeticao
            .Include(x => x.UsuarioCriador)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<RegraCompeticao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return dbContext.RegrasCompeticao.FirstOrDefaultAsync(x => x.Nome == nome, cancellationToken);
    }

    public async Task AdicionarAsync(RegraCompeticao regra, CancellationToken cancellationToken = default)
    {
        await dbContext.RegrasCompeticao.AddAsync(regra, cancellationToken);
    }

    public void Atualizar(RegraCompeticao regra)
    {
        dbContext.RegrasCompeticao.Update(regra);
    }

    public void Remover(RegraCompeticao regra)
    {
        dbContext.RegrasCompeticao.Remove(regra);
    }
}
