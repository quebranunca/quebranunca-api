using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class UnidadeTrabalho(PlataformaFutevoleiDbContext dbContext) : IUnidadeTrabalho
{
    public async Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (CriarExcecaoRegraNegocio(ex) is { } excecao)
        {
            throw excecao;
        }
    }

    public async Task ExecutarEmTransacaoAsync(
        Func<CancellationToken, Task> operacao,
        CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            await operacao(cancellationToken);
            return;
        }

        await using var transacao = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await operacao(cancellationToken);
        await transacao.CommitAsync(cancellationToken);
    }

    private static RegraNegocioException? CriarExcecaoRegraNegocio(DbUpdateException excecao)
    {
        if (excecao.InnerException is not PostgresException postgresException ||
            postgresException.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return null;
        }

        return postgresException.ConstraintName switch
        {
            "ix_grupos_atletas_grupo_id_atleta_id" =>
                new RegraNegocioException("Este atleta já está vinculado ao grupo."),
            "ix_duplas_atleta1_id_atleta2_id" =>
                new RegraNegocioException("Esta dupla já existe."),
            "ix_partidas_aprovacoes_partida_id_atleta_id" =>
                new RegraNegocioException("A aprovação desta partida já existe para o atleta."),
            _ => null
        };
    }
}
