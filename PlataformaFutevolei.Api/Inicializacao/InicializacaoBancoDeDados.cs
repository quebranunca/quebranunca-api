using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Api.Inicializacao;

internal static class InicializacaoBancoDeDados
{
    public static async Task PrepararAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        var validarConexao = app.Configuration.GetValue("Database:ValidateOnStartup", true);
        var aplicarMigrations = app.Configuration.GetValue("Database:MigrateOnStartup", true);
        var ambienteCritico = app.Environment.IsStaging() || app.Environment.IsProduction();

        if (!validarConexao && !aplicarMigrations)
        {
            app.Logger.LogInformation(
                "Validação de conexão e aplicação de migrations desabilitadas por configuração.");
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlataformaFutevoleiDbContext>();

        try
        {
            if (validarConexao)
            {
                app.Logger.LogInformation("Validando conexão com o banco de dados...");

                var connectionString = dbContext.Database.GetConnectionString();
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("Connection string vazia ou não configurada para o DbContext.");
                }

                if (ContemPlaceholderVariavel(connectionString))
                {
                    throw new InvalidOperationException(
                        "Connection string contém placeholder não resolvido (ex.: ${{...}}). " +
                        "Revise as variáveis de ambiente do Railway e as referências ao serviço Postgres.");
                }

                RegistrarResumoConexao(app, connectionString);

                try
                {
                    await dbContext.Database.OpenConnectionAsync(cancellationToken);
                    await dbContext.Database.CloseConnectionAsync();
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
                {
                    throw new InvalidOperationException(
                        "Não foi possível conectar ao PostgreSQL: autenticação falhou (usuário/senha inválidos). " +
                        "No Railway, confirme se a API está apontando para o Postgres correto e use DATABASE_URL/PGPASSWORD do mesmo serviço.",
                        ex);
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidAuthorizationSpecification)
                {
                    throw new InvalidOperationException(
                        "Não foi possível conectar ao PostgreSQL: usuário inválido para autenticação. " +
                        "Revise PGUSER/Username na connection string da API.",
                        ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Não foi possível conectar ao PostgreSQL. Verifique host/porta/credenciais e se o banco está online.",
                        ex);
                }

                if (ContemPlaceholderVariavel(connectionString))
                {
                    throw new InvalidOperationException(
                        "Connection string contém placeholder não resolvido (ex.: ${{...}}). " +
                        "Revise as variáveis de ambiente do Railway e as referências ao serviço Postgres.");
                }

                RegistrarResumoConexao(app, connectionString);

                await ValidarConexaoComRetryAsync(app.Logger, dbContext, cancellationToken);

                app.Logger.LogInformation("Conexão com o banco de dados validada com sucesso.");
            }
            else
            {
                app.Logger.LogInformation("Validação de conexão com o banco desabilitada por configuração.");
            }

            var migrationsPendentes = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            if (migrationsPendentes.Length > 0 && !aplicarMigrations)
            {
                var mensagem = $"Existem {migrationsPendentes.Length} migration(s) pendente(s): {string.Join(", ", migrationsPendentes)}.";
                if (ambienteCritico)
                {
                    throw new InvalidOperationException(
                        $"{mensagem} Aplique as migrations antes de subir a aplicação neste ambiente.");
                }

                app.Logger.LogWarning("{Mensagem}", mensagem);
            }

            if (aplicarMigrations)
            {
                app.Logger.LogInformation("Aplicando migrations pendentes...");
                await dbContext.Database.MigrateAsync(cancellationToken);
                app.Logger.LogInformation("Migrations aplicadas com sucesso.");
            }
            else
            {
                app.Logger.LogInformation("Execução de migrations na inicialização desabilitada por configuração.");
            }
        }
        catch (Exception ex)
        {
            if (!ambienteCritico)
            {
                app.Logger.LogError(ex, "Falha ao preparar banco de dados na inicialização.");
                return;
            }

            app.Logger.LogCritical(ex, "Falha crítica ao preparar banco de dados na inicialização.");
            throw;
        }
    }

    private static void RegistrarResumoConexao(WebApplication app, string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            app.Logger.LogInformation(
                "Resumo conexão PostgreSQL: Host={Host}; Porta={Porta}; Banco={Banco}; Usuário={Usuario}; SSL={SslMode}.",
                builder.Host,
                builder.Port,
                builder.Database,
                builder.Username,
                builder.SslMode);
        }
        catch
        {
            app.Logger.LogWarning("Não foi possível extrair resumo da connection string para diagnóstico.");
        }
    }

    private static bool ContemPlaceholderVariavel(string connectionString)
    {
        return connectionString.Contains("${{", StringComparison.Ordinal) ||
               connectionString.Contains("}}", StringComparison.Ordinal);
    }
}
