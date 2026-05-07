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

    public static string MontarHtml(Usuario usuario, string codigo, string? urlAppBase = null)
    {
        var email = EmailQnfTemplate.Html(usuario.Email);
        var cardCodigo = EmailQnfTemplate.MontarCardCodigo("Código de acesso", codigo);
        var conteudo = $"""
            <tr>
              <td style="padding:0 24px 8px;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse; background:#171b22; border:1px solid #342b16; border-radius:18px;">
                  <tr>
                    <td style="padding:22px; color:#f6edd8; font-size:16px; line-height:24px;">
                      <div style="font-size:12px; line-height:16px; color:#f4b018; font-weight:800; text-transform:uppercase; letter-spacing:.8px;">
                        Acesso rápido
                      </div>
                      <p style="margin:12px 0 0;">
                        Recebemos uma solicitação para entrar no QuebraNunca com o e-mail <strong style="color:#fff8e8;">{email}</strong>.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>

            <tr>
              <td style="padding:14px 24px 8px;">
                {cardCodigo}
              </td>
            </tr>

            <tr>
              <td style="padding:14px 24px 26px; color:#b8bfca; font-size:14px; line-height:22px;">
                <p style="margin:0 0 8px;"><strong style="color:#fff8e8;">Validade:</strong> este código expira em 15 minutos.</p>
                <p style="margin:0; color:#929ba8;">Se você não solicitou esse acesso, ignore este e-mail.</p>
              </td>
            </tr>
            """;

        return EmailQnfTemplate.MontarHtml(new EmailQnfTemplateOpcoes(
            "Código de acesso QuebraNunca Futevôlei",
            "Seu código para entrar no QuebraNunca.",
            urlAppBase,
            "Código de acesso",
            "Seu código de acesso",
            "Use o código abaixo para entrar na plataforma.",
            conteudo));
    }
}
