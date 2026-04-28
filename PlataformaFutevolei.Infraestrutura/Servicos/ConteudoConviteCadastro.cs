using System.Net;
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
                "Você foi convidado(a) para usar a Plataforma QuebraNunca Futevôlei como atleta.",
                "Preparamos um link pessoal para você confirmar seu acesso.",
                string.Empty,
                $"E-mail liberado para o convite: {conviteCadastro.Email}",
                $"Código do convite: {codigoConvite}",
                string.Empty,
                "Abra o link abaixo, informe o código do convite e conclua seu cadastro:",
                linkConvite,
                string.Empty,
                "Importante: este link é individual e só permite concluir o acesso com o e-mail convidado."
            ]);
    }

    public static string MontarHtmlEmail(ConviteCadastro conviteCadastro, string linkConvite, string codigoConvite)
    {
        var email = WebUtility.HtmlEncode(conviteCadastro.Email);
        var link = WebUtility.HtmlEncode(linkConvite);
        var codigoConviteCodificado = WebUtility.HtmlEncode(codigoConvite);
        var validade = WebUtility.HtmlEncode(conviteCadastro.ExpiraEmUtc.ToString("dd/MM/yyyy 'às' HH:mm 'UTC'"));

        return $"""
            <!doctype html>
            <html lang="pt-BR">
              <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Convite QuebraNunca Futevôlei</title>
              </head>
              <body style="margin:0; padding:0; background:#0b0d10; color:#171717;">
                <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                  Seu convite para acessar a Plataforma QuebraNunca Futevôlei.
                </div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; background:#0b0d10; border-collapse:collapse;">
                  <tr>
                    <td align="center" style="padding:32px 14px; font-family:Arial, Helvetica, sans-serif;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; max-width:640px; border-collapse:collapse;">
                        <tr>
                          <td style="padding:0 4px 16px;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                              <tr>
                                <td style="vertical-align:middle;">
                                  <div style="display:inline-block; width:46px; height:46px; line-height:46px; text-align:center; border-radius:14px; background:#ffb400; color:#111111; font-size:18px; font-weight:800; letter-spacing:0;">
                                    QN
                                  </div>
                                </td>
                                <td style="vertical-align:middle; padding-left:12px;">
                                  <div style="font-size:12px; line-height:16px; color:#c8c8c8; text-transform:uppercase; letter-spacing:1px;">
                                    Plataforma
                                  </div>
                                  <div style="font-size:22px; line-height:26px; color:#f6f2ea; font-weight:800;">
                                    QuebraNunca Futevôlei
                                  </div>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>

                        <tr>
                          <td style="background:#ffffff; border:1px solid #ded7c7; border-radius:16px; overflow:hidden; box-shadow:0 14px 34px rgba(0,0,0,0.22);">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                              <tr>
                                <td style="background:#171a20; padding:26px 24px; border-bottom:4px solid #ffb400;">
                                  <div style="font-size:13px; line-height:18px; color:#ffb400; font-weight:700; text-transform:uppercase; letter-spacing:1px;">
                                    Convite de acesso
                                  </div>
                                  <h1 style="margin:8px 0 0; color:#ffffff; font-size:28px; line-height:34px; font-weight:800;">
                                    Entre para acompanhar seus jogos
                                  </h1>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:26px 24px 8px; color:#171717; font-size:16px; line-height:24px;">
                                  <p style="margin:0 0 14px;">Olá!</p>
                                  <p style="margin:0 0 14px;">
                                    Você foi convidado(a) para acessar a <strong>Plataforma QuebraNunca Futevôlei</strong> como atleta.
                                  </p>
                                  <p style="margin:0;">
                                    Use o botão abaixo e informe o código do convite para confirmar seu primeiro acesso.
                                  </p>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:18px 24px 8px;">
                                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse; background:#f7f4ed; border:1px solid #ded7c7; border-radius:12px;">
                                    <tr>
                                      <td style="padding:18px;">
                                        <div style="font-family:Arial, Helvetica, sans-serif; font-size:13px; line-height:18px; color:#575757; font-weight:700; text-transform:uppercase; letter-spacing:.8px;">
                                          Código do convite
                                        </div>
                                        <div style="margin-top:8px; font-size:30px; line-height:36px; color:#111111; font-weight:800; letter-spacing:3px;">
                                          {codigoConviteCodificado}
                                        </div>
                                      </td>
                                    </tr>
                                  </table>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:18px 24px 10px;">
                                  <a href="{link}" style="display:inline-block; background:#ffb400; color:#111111; text-decoration:none; border-radius:10px; padding:14px 18px; font-family:Arial, Helvetica, sans-serif; font-size:16px; line-height:20px; font-weight:800;">
                                    Abrir convite
                                  </a>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:8px 24px 24px; color:#575757; font-size:14px; line-height:22px;">
                                  <p style="margin:0 0 8px;"><strong style="color:#171717;">E-mail liberado:</strong> {email}</p>
                                  <p style="margin:0 0 8px;"><strong style="color:#171717;">Validade:</strong> {validade}</p>
                                  <p style="margin:0;">
                                    Este convite é individual e só permite concluir o acesso com o e-mail convidado.
                                  </p>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>

                        <tr>
                          <td style="padding:18px 4px 0; color:#c8c8c8; font-family:Arial, Helvetica, sans-serif; font-size:13px; line-height:20px;">
                            <p style="margin:0 0 8px;">Se o botão não funcionar, copie e cole este endereço no navegador:</p>
                            <a href="{link}" style="color:#ffb400; text-decoration:underline; word-break:break-all;">{link}</a>
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
