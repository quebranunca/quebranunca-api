namespace PlataformaFutevolei.Dominio.Entidades;

public class SessaoUsuario : EntidadeBase
{
    public Guid UsuarioId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? UltimoUsoEmUtc { get; set; }
    public DateTime? RevogadaEmUtc { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public bool EstaAtiva(DateTime agoraUtc)
        => RevogadaEmUtc is null && ExpiraEmUtc >= agoraUtc;

    public void Revogar(DateTime agoraUtc)
    {
        RevogadaEmUtc ??= agoraUtc;
        AtualizarDataModificacao();
    }
}
