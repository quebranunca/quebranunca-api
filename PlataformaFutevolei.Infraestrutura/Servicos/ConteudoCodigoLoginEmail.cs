using System.Net;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

internal static class ConteudoCodigoLoginEmail
{
    public static string MontarAssunto()
    {
        return "Seu código para entrar na Plataforma QuebraNunca Futevôlei";
    }

    public static string MontarTexto(Usuario usuario, string codigo)
    {
        return string.Join(
            "\n",
            [
                "Olá!",
                string.Empty,
                $"Recebemos uma solicitação para entrar na Plataforma QuebraNunca Futevôlei com o e-mail {usuario.Email}.",
                "Use o código abaixo para concluir o login:",
                string.Empty,
                codigo,
                string.Empty,
                "Este código expira em 15 minutos.",
                "Se você não solicitou este acesso, ignore esta mensagem."
            ]);
    }

    public static string MontarHtml(Usuario usuario, string codigo)
    {
        var email = WebUtility.HtmlEncode(usuario.Email);
        var codigoCodificado = WebUtility.HtmlEncode(codigo);

        return $"""
            <div style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.6;">
              <p>Olá!</p>
              <p>Recebemos uma solicitação para entrar na <strong>Plataforma QuebraNunca Futevôlei</strong> com o e-mail <strong>{email}</strong>.</p>
              <p>Use o código abaixo para concluir o login:</p>
              <p style="font-size: 28px; font-weight: 700; letter-spacing: 4px;">{codigoCodificado}</p>
              <p>Este código expira em 15 minutos.</p>
              <p>Se você não solicitou este acesso, ignore esta mensagem.</p>
            </div>
            """;
    }
}
