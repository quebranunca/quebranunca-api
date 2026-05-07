using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class UnidadeTrabalho(PlataformaFutevoleiDbContext dbContext) : IUnidadeTrabalho
{
    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecutarEmTransacaoAsync(
        Func<CancellationToken, Task> operacao,
        CancellationToken cancellationToken = default)
    {
        await using var transacao = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await operacao(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }
}
