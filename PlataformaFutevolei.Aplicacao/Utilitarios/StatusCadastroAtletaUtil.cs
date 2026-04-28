using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public static class StatusCadastroAtletaUtil
{
    public const string StatusAtivo = "Ativo";
    public const string StatusPendenteComContato = "Pendente com contato";
    public const string StatusPendenteSemContato = "Pendente sem contato";

    public static bool PossuiUsuarioVinculado(Atleta atleta)
    {
        return atleta.Usuario is not null;
    }

    public static bool TemEmail(Atleta atleta)
    {
        return !string.IsNullOrWhiteSpace(atleta.Email);
    }

    public static string ObterStatusPendencia(Atleta atleta)
    {
        if (PossuiUsuarioVinculado(atleta))
        {
            return StatusAtivo;
        }

        return TemEmail(atleta)
            ? StatusPendenteComContato
            : StatusPendenteSemContato;
    }
}
