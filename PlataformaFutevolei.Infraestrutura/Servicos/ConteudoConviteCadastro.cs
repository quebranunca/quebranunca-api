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
                $"Você foi convidado(a) para jogar no {EmailQnfTemplate.NomeMarca}.",
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
        var email = EmailQnfTemplate.Html(conviteCadastro.Email);
        var link = EmailQnfTemplate.Html(linkConvite);
        var validade = EmailQnfTemplate.Html(FormatarValidade(conviteCadastro.ExpiraEmUtc));
        var organizador = EmailQnfTemplate.Html(ObterNomeOrganizador(conviteCadastro));
        var cardCodigo = EmailQnfTemplate.MontarCardCodigo("Código do convite", codigoConvite);
        var conteudo = $"""
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
                {cardCodigo}
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
            """;
        var rodape = $"""
            <p style="margin:0 0 8px;">Se o botão não funcionar, copie e cole este endereço no navegador:</p>
            <a href="{link}" style="color:#ffca4b; text-decoration:underline; word-break:break-all;">{link}</a>
            """;

        return EmailQnfTemplate.MontarHtml(new EmailQnfTemplateOpcoes(
            "Convite QuebraNunca Futevôlei",
            "Você foi convidado para jogar no QuebraNunca. Entre no grupo e acompanhe seu ranking.",
            linkConvite,
            "Convite de acesso",
            "Você foi convidado para jogar no QuebraNunca",
            "Registre partidas, acompanhe rankings e evolua no futevôlei.",
            conteudo,
            rodape));
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
