using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace PlataformaFutevolei.Infraestrutura.Persistencia;

public class PlataformaFutevoleiDbContextFactory : IDesignTimeDbContextFactory<PlataformaFutevoleiDbContext>
{
    public PlataformaFutevoleiDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Padrao");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string não configurada para o design-time do EF Core. " +
                "Defina ConnectionStrings__DefaultConnection (ou ConnectionStrings__Padrao) antes de executar comandos de migration.");
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!connectionString.Contains("Ssl Mode", StringComparison.OrdinalIgnoreCase))
        {
            connectionStringBuilder.SslMode = SslMode.Require;
        }

        var optionsBuilder = new DbContextOptionsBuilder<PlataformaFutevoleiDbContext>();
        optionsBuilder.UseNpgsql(connectionStringBuilder.ConnectionString);

        return new PlataformaFutevoleiDbContext(optionsBuilder.Options);
    }
}
