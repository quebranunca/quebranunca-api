using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class CategoriaCompeticaoRepositorio(PlataformaFutevoleiDbContext dbContext) : ICategoriaCompeticaoRepositorio
{
    public async Task<IReadOnlyList<CategoriaCompeticao>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CategoriasCompeticao
            .AsNoTracking()
            .Include(x => x.Competicao)
                .ThenInclude(x => x.FormatoCampeonato)
            .Include(x => x.Competicao)
                .ThenInclude(x => x.RegraCompeticao)
            .Include(x => x.FormatoCampeonato)
            .Include(x => x.Inscricoes)
            .Where(x => x.CompeticaoId == competicaoId)
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<CategoriaCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.CategoriasCompeticao
            .Include(x => x.Competicao)
                .ThenInclude(x => x.FormatoCampeonato)
            .Include(x => x.Competicao)
                .ThenInclude(x => x.RegraCompeticao)
            .Include(x => x.FormatoCampeonato)
            .Include(x => x.Inscricoes)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AdicionarAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken = default)
    {
        await dbContext.CategoriasCompeticao.AddAsync(categoria, cancellationToken);
    }

    public void Atualizar(CategoriaCompeticao categoria)
    {
        dbContext.CategoriasCompeticao.Update(categoria);
    }

    public void Remover(CategoriaCompeticao categoria)
    {
        dbContext.CategoriasCompeticao.Remove(categoria);
    }
}
