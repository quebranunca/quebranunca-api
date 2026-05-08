using System.Net;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

internal record EmailQnfTemplateOpcoes(
    string TituloDocumento,
    string PreHeader,
    string? UrlAppBase,
    string Selo,
    string Titulo,
    string Subtitulo,
    string ConteudoHtml,
    string? RodapeHtml = null
);

internal static class EmailQnfTemplate
{
    public const string NomeMarca = "QuebraNunca Futevôlei";
    public const string InstagramMarca = "@quebranuncaftv";
    public const string SiteMarca = "quebranunca.com.br";
    public const string FraseMarca = "Mais um jogo na conta.";

    private const string CaminhoLogoLight = "/branding/logo-qnf-light.png";

    public static string MontarUrlLogoLight(string? urlBase)
    {
        return MontarUrlAsset(urlBase, CaminhoLogoLight);
    }

    public static string Html(string? valor)
    {
        return WebUtility.HtmlEncode(valor ?? string.Empty);
    }

    public static string MontarCardCodigo(string rotulo, string codigo)
    {
        var rotuloHtml = Html(rotulo);
        var codigoHtml = Html(codigo);

        return $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#0b0d10" style="width:100%; border-collapse:collapse; background-color:#0b0d10; border:1px solid #3f3110; border-radius:16px;">
              <tr>
                <td align="center" style="padding:20px 12px;">
                  <div style="font-size:12px; line-height:16px; color:#ffca4b; font-weight:800; text-transform:uppercase; letter-spacing:1px;">
                    {rotuloHtml}
                  </div>
                  <div style="margin-top:10px; color:#fff8e8; font-size:38px; line-height:44px; font-weight:900; letter-spacing:5px;">
                    {codigoHtml}
                  </div>
                </td>
              </tr>
            </table>
            """;
    }

    public static string MontarHtml(EmailQnfTemplateOpcoes opcoes)
    {
        var tituloDocumento = Html(opcoes.TituloDocumento);
        var preHeader = Html(opcoes.PreHeader);
        var logoUrl = Html(MontarUrlAsset(opcoes.UrlAppBase, CaminhoLogoLight));
        var selo = Html(opcoes.Selo);
        var titulo = Html(opcoes.Titulo);
        var subtitulo = Html(opcoes.Subtitulo);
        var rodapeHtml = string.IsNullOrWhiteSpace(opcoes.RodapeHtml)
            ? string.Empty
            : opcoes.RodapeHtml;

        return $"""
            <!doctype html>
            <html lang="pt-BR">
              <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="color-scheme" content="light only">
                <meta name="supported-color-schemes" content="light">
                <title>{tituloDocumento}</title>
              </head>
              <body bgcolor="#ffffff" style="margin:0; padding:0; background-color:#ffffff; color:#f8f1df;">
                <div style="display:none; max-height:0; overflow:hidden; opacity:0; color:transparent;">
                  {preHeader}
                </div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#ffffff" style="width:100%; background-color:#ffffff; border-collapse:collapse;">
                  <tr>
                    <td align="center" bgcolor="#ffffff" style="padding:24px 12px 30px; background-color:#ffffff; font-family:Arial, Helvetica, sans-serif;">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" align="center" style="width:100%; max-width:600px; border-collapse:collapse; margin:0 auto;">
                        <tr>
                          <td style="padding:0 0 18px;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#08090B" style="width:100%; border-collapse:collapse; background-color:#08090B; border:1px solid #2d2615; border-radius:20px; overflow:hidden;">
                              <tr>
                                <td style="padding:0;">
                                  <div style="height:6px; line-height:6px; background:#ffb300;">&nbsp;</div>
                                </td>
                              </tr>
                              <tr>
                                <td align="center" style="padding:32px 24px 30px;">
                                  <table role="presentation" cellpadding="0" cellspacing="0" style="border-collapse:collapse; margin:0 auto;">
                                    <tr>
                                      <td align="center" style="width:124px; height:124px; border-radius:999px; background:#0b0d10; border:1px solid #3f3110; padding:10px;">
                                        <img src="{logoUrl}" width="104" height="104" alt="QuebraNunca Futevôlei" style="display:block; width:104px; height:104px; object-fit:contain; border:0; outline:none; text-decoration:none;">
                                      </td>
                                    </tr>
                                  </table>
                                  <div style="margin-top:20px; color:#ffb300; font-size:12px; line-height:16px; font-weight:900; text-transform:uppercase; letter-spacing:1px;">
                                    QuebraNunca Futevôlei
                                  </div>
                                  <div style="margin-top:8px; color:#fff8e8; font-size:28px; line-height:34px; font-weight:900;">
                                    Experiência premium dentro e fora da quadra
                                  </div>
                                  <div style="margin-top:10px; color:#b8bfca; font-size:14px; line-height:22px;">
                                    @quebranuncaftv · quebranunca.com.br
                                  </div>
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>

                        <tr>
                          <td bgcolor="#08090B" style="background-color:#08090B; border:1px solid #2d2615; border-radius:20px; overflow:hidden;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#08090B" style="width:100%; border-collapse:collapse; background-color:#08090B;">
                              <tr>
                                <td style="padding:0;">
                                  <div style="height:6px; line-height:6px; background:#ffb300;">&nbsp;</div>
                                </td>
                              </tr>

                              <tr>
                                <td bgcolor="#101318" style="padding:30px 24px 22px; background-color:#101318;">
                                  <div style="display:inline-block; padding:7px 12px; border-radius:999px; background:#221a08; border:1px solid #6b5012; color:#ffca4b; font-size:12px; line-height:16px; font-weight:800; text-transform:uppercase; letter-spacing:.8px;">
                                    {selo}
                                  </div>
                                  <h1 style="margin:18px 0 0; color:#fff8e8; font-size:32px; line-height:38px; font-weight:900;">
                                    {titulo}
                                  </h1>
                                  <p style="margin:12px 0 0; color:#d8d0bd; font-size:16px; line-height:24px;">
                                    {subtitulo}
                                  </p>
                                </td>
                              </tr>

                              {opcoes.ConteudoHtml}
                            </table>
                          </td>
                        </tr>

                        <tr>
                          <td align="center" style="padding:20px 8px 0; color:#a5adba; font-family:Arial, Helvetica, sans-serif; font-size:13px; line-height:20px;">
                            <p style="margin:0 0 6px; color:#fff8e8; font-weight:800;">{FraseMarca}</p>
                            <p style="margin:0 0 14px;">{InstagramMarca} · {SiteMarca}</p>
                            {rodapeHtml}
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

    private static string MontarUrlAsset(string? urlBase, string caminhoAsset)
    {
        if (!Uri.TryCreate(urlBase, UriKind.Absolute, out var uri))
        {
            return caminhoAsset;
        }

        var porta = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Scheme}://{uri.Host}{porta}{caminhoAsset}";
    }
}
