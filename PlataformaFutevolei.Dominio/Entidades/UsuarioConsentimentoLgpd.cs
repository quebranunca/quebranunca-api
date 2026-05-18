namespace PlataformaFutevolei.Dominio.Entidades;

public class UsuarioConsentimentoLgpd : EntidadeBase
{
    public Guid UsuarioId { get; set; }
    public string VersaoPoliticaPrivacidade { get; set; } = string.Empty;
    public bool AceitouPoliticaPrivacidade { get; set; }
    public bool AceitouTermosUso { get; set; }
    public bool AceitouUsoLocalizacao { get; set; }
    public bool AceitouUsoImagem { get; set; }
    public DateTime AceitoEm { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public Usuario? Usuario { get; set; }
}
