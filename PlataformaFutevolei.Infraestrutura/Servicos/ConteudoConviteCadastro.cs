using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

internal static class ConteudoConviteCadastro
{
    public static string MontarLinkConvite(string urlAppBase, string identificadorPublico)
    {
        return $"{urlAppBase.TrimEnd('/')}/cadastro/convite/{Uri.EscapeDataString(identificadorPublico)}";
    }

    public static string MontarAssuntoEmail()
    {
        return "Seu convite para acessar a Plataforma QuebraNunca Futevôlei";
    }

    public static string MontarTextoEmail(ConviteCadastro conviteCadastro, string linkConvite, string codigoConvite)
    {
        return string.Join(
            "\n",
            [
                "Olá!",
                string.Empty,
                $"Você foi convidado(a) para acessar a Plataforma {EmailQnfTemplate.NomeMarca}.",
                $"{ObterNomeOrganizador(conviteCadastro)} convidou você para participar da plataforma e registrar partidas, acompanhar rankings e evoluir no futevôlei.",
                string.Empty,
                "Acesse a plataforma:",
                linkConvite,
                string.Empty,
                $"Código do convite: {codigoConvite}",
                string.Empty,
                "Este convite pode expirar futuramente.",
                "Se você não esperava este convite, ignore este e-mail."
            ]);
    }

    public static string MontarHtmlEmail(ConviteCadastro conviteCadastro, string linkConvite, string codigoConvite)
    {
        var link = EmailQnfTemplate.Html(linkConvite);
        var logoUrl = EmailQnfTemplate.Html(EmailQnfTemplate.MontarUrlLogoLight(linkConvite));
        var organizador = EmailQnfTemplate.Html(ObterNomeOrganizador(conviteCadastro));
        var codigo = EmailQnfTemplate.Html(codigoConvite);

        return $"""
            <!doctype html>
            <html lang="pt-BR">
              <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="color-scheme" content="light only">
                <meta name="supported-color-schemes" content="light">
                <title>Convite QuebraNunca Futevôlei</title>
              </head>
              <body bgcolor="#ffffff" style="margin:0; padding:0; background-color:#ffffff; color:#101318;">
                <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                  Você foi convidado para acessar a Plataforma QuebraNunca Futevôlei.
                </div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#ffffff" style="width:100%; background-color:#ffffff; border-collapse:collapse;">
                  <tr>
                    <td align="center" bgcolor="#ffffff" style="padding:16px 12px; background-color:#ffffff; font-family:Arial, Helvetica, sans-serif;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" align="center" bgcolor="#08090B" style="width:100%; max-width:520px; border-collapse:collapse; margin:0 auto; background-color:#08090B; border:1px solid #2d2615; border-radius:16px;">
                        <tr>
                          <td style="padding:0;">
                            <div style="height:4px; line-height:4px; background:#ffb300;">&nbsp;</div>
                          </td>
                        </tr>
                        <tr>
                          <td align="center" style="padding:18px 20px 22px;">
                            <img src="{logoUrl}" width="52" height="52" alt="QuebraNunca Futevôlei" style="display:block; width:52px; height:52px; object-fit:contain; border:0; outline:none; text-decoration:none; margin:0 auto 14px;">

                            <h1 style="margin:0; color:#fff8e8; font-size:24px; line-height:30px; font-weight:900;">
                              Você foi convidado
                            </h1>

                            <p style="margin:10px 0 0; color:#f6edd8; font-size:16px; line-height:23px;">
                              Você foi convidado para acessar a Plataforma QuebraNunca Futevôlei.
                            </p>

                            <p style="margin:10px 0 16px; color:#b8bfca; font-size:14px; line-height:21px;">
                              <strong style="color:#fff8e8;">{organizador}</strong> convidou você para participar da plataforma e registrar partidas, acompanhar rankings e evoluir no futevôlei.
                            </p>

                            <a href="{link}" style="display:block; width:100%; box-sizing:border-box; background:#ffb300; color:#0b0d10; text-decoration:none; border-radius:14px; padding:16px 18px; font-family:Arial, Helvetica, sans-serif; font-size:17px; line-height:22px; font-weight:900; text-align:center;">
                              Acessar Plataforma
                            </a>

                            <p style="margin:12px 0 0; color:#d8d0bd; font-size:13px; line-height:19px;">
                              Se solicitado, informe o código <strong style="color:#ffca4b;">{codigo}</strong>.
                            </p>

                            <p style="margin:14px 0 0; color:#929ba8; font-size:12px; line-height:18px;">
                              Se o botão não funcionar:<br>
                              <a href="{link}" style="color:#ffca4b; text-decoration:underline; word-break:break-all;">{link}</a>
                            </p>

                            <p style="margin:10px 0 0; color:#777f8c; font-size:12px; line-height:18px;">
                              Este convite pode expirar futuramente.
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

    private static string ObterNomeOrganizador(ConviteCadastro conviteCadastro)
    {
        return string.IsNullOrWhiteSpace(conviteCadastro.CriadoPorUsuario?.Nome)
            ? "Equipe QuebraNunca"
            : conviteCadastro.CriadoPorUsuario.Nome.Trim();
    }

    public static string MontarTextoWhatsapp(ConviteCadastro conviteCadastro, string linkConvite, string codigoConvite)
    {
        return string.Join(
            "\n",
            [
                "Olá!",
                string.Empty,
                "Você recebeu um convite para acessar a Plataforma QuebraNunca Futevôlei como atleta.",
                "Use o link abaixo e o código do convite para confirmar seu primeiro acesso:",
                linkConvite,
                string.Empty,
                $"Código do convite: {codigoConvite}",
                $"E-mail liberado para o convite: {conviteCadastro.Email}",
                "Importante: este link é individual e só funciona com o e-mail convidado."
            ]);
    }
}
