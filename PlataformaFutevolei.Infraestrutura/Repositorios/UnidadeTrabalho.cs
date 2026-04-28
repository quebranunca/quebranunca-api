using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class UnidadeTrabalho(PlataformaFutevoleiDbContext dbContext) : IUnidadeTrabalho
{
    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
