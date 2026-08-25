using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Seguranca;

public sealed class ProtecaoAbusoDistribuidaPostgres(
    PlataformaFutevoleiDbContext dbContext,
    ILogger<ProtecaoAbusoDistribuidaPostgres> logger)
    : IProtecaoAbusoDistribuida
{
    private static int operacoes;

    public async Task<bool> TentarConsumirAsync(
        string chave,
        int limite,
        TimeSpan janela,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        var expiraEm = agora.Add(janela);
        var conexao = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        var deveFechar = conexao.State != ConnectionState.Open;
        if (deveFechar)
        {
            await conexao.OpenAsync(cancellationToken);
        }

        try
        {
            await using var comando = conexao.CreateCommand();
            comando.CommandText = """
                INSERT INTO controles_rate_limit (chave, janela_inicio_utc, contador, expira_em_utc)
                VALUES (@chave, @agora, 1, @expira)
                ON CONFLICT (chave) DO UPDATE SET
                    janela_inicio_utc = CASE
                        WHEN controles_rate_limit.expira_em_utc <= @agora THEN @agora
                        ELSE controles_rate_limit.janela_inicio_utc
                    END,
                    contador = CASE
                        WHEN controles_rate_limit.expira_em_utc <= @agora THEN 1
                        ELSE controles_rate_limit.contador + 1
                    END,
                    expira_em_utc = CASE
                        WHEN controles_rate_limit.expira_em_utc <= @agora THEN @expira
                        ELSE controles_rate_limit.expira_em_utc
                    END
                RETURNING contador;
                """;
            comando.Parameters.AddWithValue("chave", chave);
            comando.Parameters.AddWithValue("agora", agora);
            comando.Parameters.AddWithValue("expira", expiraEm);
            var contador = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken));
            if ((Interlocked.Increment(ref operacoes) & 255) == 0)
            {
                await LimparRegistrosExpiradosAsync(conexao, agora);
            }
            return contador <= limite;
        }
        finally
        {
            if (deveFechar)
            {
                await conexao.CloseAsync();
            }
        }
    }

    private async Task LimparRegistrosExpiradosAsync(NpgsqlConnection conexao, DateTime agoraUtc)
    {
        try
        {
            await using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM controles_rate_limit WHERE expira_em_utc <= @agora;";
            comando.Parameters.AddWithValue("agora", agoraUtc);
            await comando.ExecuteNonQueryAsync(CancellationToken.None);
        }
        catch (Exception excecao)
        {
            logger.LogWarning(excecao, "Não foi possível limpar controles de rate limit expirados.");
        }
    }
}
