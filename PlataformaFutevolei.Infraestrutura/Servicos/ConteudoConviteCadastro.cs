using System.Net;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

internal static class ConteudoConviteCadastro
{
    private const string NomeMarca = "QuebraNunca Futevôlei";
    private const string InstagramMarca = "@quebranuncaftv";
    private const string SiteMarca = "quebranunca.com.br";
    private const string FraseMarca = "Mais um jogo na conta.";
    private const string CaminhoLogoLight = "/branding/logo-qnf-light.svg";

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
                $"Você foi convidado(a) para jogar no {NomeMarca}.",
                "Entre no grupo, registre partidas e acompanhe sua evolução no ranking.",
                string.Empty,
                $"Organizador: {ObterNomeOrganizador(conviteCadastro)}",
                $"E-mail liberado para o convite: {conviteCadastro.Email}",
                $"Código do convite: {codigoConvite}",
                $"Validade: {FormatarValidade(conviteCadastro.ExpiraEmUtc)}",
                string.Empty,
                "Abra o link abaixo, informe o código do convite e conclua seu cadastro:",
                linkConvite,
                string.Empty,
                "Importante: este link é individual e só permite concluir o acesso com o e-mail convidado."
            ]);
    }

    public static string MontarHtmlEmail(ConviteCadastro conviteCadastro, string linkConvite, string codigoConvite)
    {
        var email = Html(conviteCadastro.Email);
        var link = Html(linkConvite);
        var codigoConviteCodificado = Html(codigoConvite);
        var validade = Html(FormatarValidade(conviteCadastro.ExpiraEmUtc));
        var organizador = Html(ObterNomeOrganizador(conviteCadastro));
        var logoUrl = Html(MontarUrlAsset(linkConvite, CaminhoLogoLight));

        return $"""
            <!doctype html>
            <html lang="pt-BR">
              <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Convite QuebraNunca Futevôlei</title>
              </head>
              <body style="margin:0; padding:0; background:#07080b; color:#f8f1df;">
                <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                  Você foi convidado para jogar no QuebraNunca. Entre no grupo e acompanhe seu ranking.
                </div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; background:#07080b; border-collapse:collapse;">
                  <tr>
                    <td align="center" style="padding:24px 12px 30px; font-family:Arial, Helvetica, sans-serif;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="width:100%; max-width:640px; border-collapse:collapse;">
                        <tr>
                          <td align="center" style="padding:0 0 22px;">
                            <img src="{logoUrl}" width="76" height="76" alt="QuebraNunca Futevôlei" style="display:block; width:76px; height:76px; object-fit:contain; border:0; outline:none; text-decoration:none;">
                          </td>
                        </tr>

                        <tr>
                          <td style="background:#111419; border:1px solid #2d2615; border-radius:24px; overflow:hidden; box-shadow:0 22px 60px rgba(0,0,0,0.42);">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                              <tr>
                                <td style="padding:0;">
                                  <div style="height:6px; line-height:6px; background:#ffb300;">&nbsp;</div>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:30px 24px 22px; background:#101318;">
                                  <div style="display:inline-block; padding:7px 12px; border-radius:999px; background:#221a08; border:1px solid #6b5012; color:#ffca4b; font-size:12px; line-height:16px; font-weight:800; text-transform:uppercase; letter-spacing:.8px;">
                                    Convite de acesso
                                  </div>
                                  <h1 style="margin:18px 0 0; color:#fff8e8; font-size:32px; line-height:38px; font-weight:900;">
                                    Você foi convidado para jogar no QuebraNunca
                                  </h1>
                                  <p style="margin:12px 0 0; color:#d8d0bd; font-size:16px; line-height:24px;">
                                    Registre partidas, acompanhe rankings e evolua no futevôlei.
                                  </p>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:0 24px 8px;">
                                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse; background:#171b22; border:1px solid #342b16; border-radius:18px;">
                                    <tr>
                                      <td style="padding:22px; color:#f6edd8; font-size:16px; line-height:24px;">
                                        <div style="font-size:12px; line-height:16px; color:#f4b018; font-weight:800; text-transform:uppercase; letter-spacing:.8px;">
                                          QuebraNunca Futevôlei
                                        </div>
                                        <p style="margin:12px 0 14px;">
                                          Olá! Seu acesso foi liberado para entrar na plataforma, registrar seus jogos e acompanhar sua posição no ranking.
                                        </p>
                                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                                          <tr>
                                            <td style="padding:10px 0; border-top:1px solid #2f333b; color:#9ea6b3; font-size:13px; line-height:18px; text-transform:uppercase; letter-spacing:.6px;">Organizador</td>
                                            <td align="right" style="padding:10px 0; border-top:1px solid #2f333b; color:#fff8e8; font-size:14px; line-height:18px; font-weight:800;">{organizador}</td>
                                          </tr>
                                          <tr>
                                            <td style="padding:10px 0; border-top:1px solid #2f333b; color:#9ea6b3; font-size:13px; line-height:18px; text-transform:uppercase; letter-spacing:.6px;">Convite</td>
                                            <td align="right" style="padding:10px 0; border-top:1px solid #2f333b; color:#fff8e8; font-size:14px; line-height:18px; font-weight:800;">Acesso de atleta</td>
                                          </tr>
                                        </table>
                                      </td>
                                    </tr>
                                  </table>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:14px 24px 8px;">
                                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse; background:#0b0d10; border:1px solid #3f3110; border-radius:16px;">
                                    <tr>
                                      <td align="center" style="padding:18px 12px;">
                                        <div style="font-size:12px; line-height:16px; color:#ffca4b; font-weight:800; text-transform:uppercase; letter-spacing:1px;">
                                          Código do convite
                                        </div>
                                        <div style="margin-top:8px; color:#fff8e8; font-size:34px; line-height:40px; font-weight:900; letter-spacing:4px;">
                                          {codigoConviteCodificado}
                                        </div>
                                      </td>
                                    </tr>
                                  </table>
                                </td>
                              </tr>

                              <tr>
                                <td align="center" style="padding:18px 24px 8px;">
                                  <a href="{link}" style="display:block; background:#ffb300; color:#0b0d10; text-decoration:none; border-radius:14px; padding:16px 18px; font-family:Arial, Helvetica, sans-serif; font-size:16px; line-height:20px; font-weight:900; box-shadow:0 10px 24px rgba(255,179,0,0.28);">
                                    Acessar convite
                                  </a>
                                </td>
                              </tr>

                              <tr>
                                <td style="padding:14px 24px 26px; color:#b8bfca; font-size:14px; line-height:22px;">
                                  <p style="margin:0 0 14px;">
                                    Entre com o e-mail convidado e informe o código acima para concluir seu primeiro acesso.
                                  </p>
                                  <p style="margin:0 0 8px;"><strong style="color:#fff8e8;">E-mail liberado:</strong> {email}</p>
                                  <p style="margin:0 0 8px;"><strong style="color:#fff8e8;">Validade:</strong> {validade}</p>
                                  <p style="margin:0; color:#929ba8;">
                                    Este convite é individual e só permite concluir o acesso com o e-mail convidado.
                                  </p>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>

                        <tr>
                          <td align="center" style="padding:20px 8px 0; color:#a5adba; font-family:Arial, Helvetica, sans-serif; font-size:13px; line-height:20px;">
                            <p style="margin:0 0 6px; color:#fff8e8; font-weight:800;">{FraseMarca}</p>
                            <p style="margin:0 0 14px;">{InstagramMarca} · {SiteMarca}</p>
                            <p style="margin:0 0 8px;">Se o botão não funcionar, copie e cole este endereço no navegador:</p>
                            <a href="{link}" style="color:#ffca4b; text-decoration:underline; word-break:break-all;">{link}</a>
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

    private static string Html(string? valor)
    {
        return WebUtility.HtmlEncode(valor ?? string.Empty);
    }

    private static string ObterNomeOrganizador(ConviteCadastro conviteCadastro)
    {
        return string.IsNullOrWhiteSpace(conviteCadastro.CriadoPorUsuario?.Nome)
            ? "Equipe QuebraNunca"
            : conviteCadastro.CriadoPorUsuario.Nome.Trim();
    }

    private static string FormatarValidade(DateTime expiraEmUtc)
    {
        return expiraEmUtc.ToString("dd/MM/yyyy 'às' HH:mm 'UTC'");
    }

    private static string MontarUrlAsset(string linkConvite, string caminhoAsset)
    {
        if (!Uri.TryCreate(linkConvite, UriKind.Absolute, out var uri))
        {
            return caminhoAsset;
        }

        var porta = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Scheme}://{uri.Host}{porta}{caminhoAsset}";
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
