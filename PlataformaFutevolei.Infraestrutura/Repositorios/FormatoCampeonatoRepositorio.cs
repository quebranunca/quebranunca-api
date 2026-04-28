using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class FormatoCampeonatoRepositorio(PlataformaFutevoleiDbContext dbContext) : IFormatoCampeonatoRepositorio
{
    public async Task<IReadOnlyList<FormatoCampeonato>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.FormatosCampeonato
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<FormatoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.FormatosCampeonato.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<FormatoCampeonato?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return dbContext.FormatosCampeonato.FirstOrDefaultAsync(x => x.Nome == nome, cancellationToken);
    }

    public async Task AdicionarAsync(FormatoCampeonato formato, CancellationToken cancellationToken = default)
    {
        await dbContext.FormatosCampeonato.AddAsync(formato, cancellationToken);
    }

    public void Atualizar(FormatoCampeonato formato)
    {
        dbContext.FormatosCampeonato.Update(formato);
    }

    public void Remover(FormatoCampeonato formato)
    {
        dbContext.FormatosCampeonato.Remove(formato);
    }
}
