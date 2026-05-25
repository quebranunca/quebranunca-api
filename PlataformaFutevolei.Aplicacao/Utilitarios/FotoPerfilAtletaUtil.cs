using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public static class FotoPerfilAtletaUtil
{
    public static string? ObterUrlPublica(Atleta? atleta)
        => ObterUrlPublica(atleta?.Usuario);

    public static string? ObterUrlPublica(Usuario? usuario)
        => usuario?.PermitirUsoImagem == true && !string.IsNullOrWhiteSpace(usuario.FotoPerfilUrl)
            ? usuario.FotoPerfilUrl
            : null;
}
