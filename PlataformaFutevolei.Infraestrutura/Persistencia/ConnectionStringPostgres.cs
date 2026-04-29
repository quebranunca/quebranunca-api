using Npgsql;

namespace PlataformaFutevolei.Infraestrutura.Persistencia;

internal static class ConnectionStringPostgres
{
    public static string Normalizar(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var valor = connectionString.Trim();
        if (EhUrlPostgres(valor))
        {
            return ConverterUrlPostgres(valor);
        }

        return NormalizarChaveValor(valor);
    }

    private static string NormalizarChaveValor(string connectionString)
    {
        try
        {
            return CriarBuilder(connectionString).ConnectionString;
        }
        catch (ArgumentException ex) when (ParametroTrustServerCertificateRejeitado(ex))
        {
            return CriarBuilder(RemoverParametroTrustServerCertificate(connectionString)).ConnectionString;
        }
    }

    private static NpgsqlConnectionStringBuilder CriarBuilder(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!connectionString.Contains("Ssl Mode", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("sslmode", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = SslMode.Require;
        }

        return builder;
    }

    private static bool EhUrlPostgres(string connectionString)
    {
        return Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase));
    }

    private static string ConverterUrlPostgres(string connectionString)
    {
        var uri = new Uri(connectionString);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            SslMode = SslMode.Require
        };

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length > 0)
        {
            builder.Username = Uri.UnescapeDataString(userInfo[0]);
        }

        if (userInfo.Length > 1)
        {
            builder.Password = Uri.UnescapeDataString(userInfo[1]);
        }

        foreach (var parametro in ObterParametrosQuery(uri.Query))
        {
            if (string.Equals(parametro.Chave, "sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = ConverterSslMode(parametro.Valor);
            }
        }

        return builder.ConnectionString;
    }

    private static SslMode ConverterSslMode(string valor)
    {
        return valor.Trim().ToLowerInvariant() switch
        {
            "disable" => SslMode.Disable,
            "allow" => SslMode.Allow,
            "prefer" => SslMode.Prefer,
            "verify-ca" => SslMode.VerifyCA,
            "verifyfull" or "verify-full" => SslMode.VerifyFull,
            _ => SslMode.Require
        };
    }

    private static IEnumerable<(string Chave, string Valor)> ObterParametrosQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            yield break;
        }

        foreach (var parte in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var chaveValor = parte.Split('=', 2);
            var chave = Uri.UnescapeDataString(chaveValor[0]);
            var valor = chaveValor.Length > 1
                ? Uri.UnescapeDataString(chaveValor[1])
                : string.Empty;

            yield return (chave, valor);
        }
    }

    private static bool ParametroTrustServerCertificateRejeitado(ArgumentException ex)
    {
        return ex.Message.Contains("trust server certificate", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(ex.ParamName, "trust server certificate", StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoverParametroTrustServerCertificate(string connectionString)
    {
        var partes = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var partesFiltradas = partes.Where(parte =>
        {
            var chave = parte.Split('=', 2)[0];
            var chaveNormalizada = chave.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();

            return chaveNormalizada != "trustservercertificate";
        });

        return string.Join(';', partesFiltradas);
    }
}
