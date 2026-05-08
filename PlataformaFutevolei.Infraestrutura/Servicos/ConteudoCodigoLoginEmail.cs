using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

internal static class ConteudoCodigoLoginEmail
{
    public static string MontarAssunto(string codigo)
    {
        return $"Seu código QuebraNunca é {codigo}";
    }

    public static string MontarTexto(Usuario usuario, string codigo)
    {
        _ = usuario;

        return string.Join(
            "\n",
            [
                "Seu código de acesso",
                codigo,
                string.Empty,
                "Use este código para entrar na Plataforma QuebraNunca Futevôlei.",
                "Se você não solicitou este código, ignore este e-mail."
            ]);
    }

    public static string MontarHtml(Usuario usuario, string codigo, string? urlAppBase = null)
    {
        _ = usuario;

        var codigoHtml = EmailQnfTemplate.Html(codigo);
        var logoUrl = EmailQnfTemplate.Html(EmailQnfTemplate.MontarUrlLogoLight(urlAppBase));

        return $"""
            <!doctype html>
            <html lang="pt-BR">
              <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="color-scheme" content="light only">
                <meta name="supported-color-schemes" content="light">
                <title>Seu código de acesso</title>
              </head>
              <body bgcolor="#ffffff" style="margin:0; padding:0; background-color:#ffffff; color:#101318;">
                <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                  Seu código QuebraNunca é {codigoHtml}.
                </div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#ffffff" style="width:100%; background-color:#ffffff; border-collapse:collapse;">
                  <tr>
                    <td align="center" bgcolor="#ffffff" style="padding:16px 12px; background-color:#ffffff; font-family:Arial, Helvetica, sans-serif;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" align="center" bgcolor="#08090B" style="width:100%; max-width:420px; border-collapse:collapse; margin:0 auto; background-color:#08090B; border:1px solid #2d2615; border-radius:14px;">
                        <tr>
                          <td style="padding:0;">
                            <div style="height:4px; line-height:4px; background:#ffb300;">&nbsp;</div>
                          </td>
                        </tr>
                        <tr>
                          <td align="center" style="padding:18px 18px 20px;">
                            <img src="{logoUrl}" width="48" height="48" alt="QuebraNunca Futevôlei" style="display:block; width:48px; height:48px; object-fit:contain; border:0; outline:none; text-decoration:none; margin:0 auto 12px;">

                            <h1 style="margin:0; color:#fff8e8; font-size:22px; line-height:28px; font-weight:800;">
                              Seu código de acesso
                            </h1>

                            <div style="margin:14px 0 12px; color:#fff8e8; font-size:42px; line-height:48px; font-weight:900; letter-spacing:6px;">
                              {codigoHtml}
                            </div>

                            <p style="margin:0; color:#d8d0bd; font-size:15px; line-height:22px;">
                              Use este código para entrar na Plataforma QuebraNunca Futevôlei.
                            </p>

                            <p style="margin:12px 0 0; color:#929ba8; font-size:12px; line-height:18px;">
                              Se você não solicitou este código, ignore este e-mail.
                            </p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;
    }
}
