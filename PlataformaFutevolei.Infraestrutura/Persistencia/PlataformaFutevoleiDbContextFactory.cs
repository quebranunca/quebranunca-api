using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        var optionsBuilder = new DbContextOptionsBuilder<PlataformaFutevoleiDbContext>();
        optionsBuilder.UseNpgsql(ConnectionStringPostgres.Normalizar(connectionString));

        return new PlataformaFutevoleiDbContext(optionsBuilder.Options);
    }
}
