namespace PlataformaFutevolei.Dominio.Entidades;

public class IdentidadeExternaUsuario : EntidadeBase
{
    public Guid UsuarioId { get; private set; }
    public string Emissor { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public DateTime VinculadaEmUtc { get; private set; }
    public DateTime UltimoLoginEmUtc { get; private set; }
    public Usuario Usuario { get; private set; } = null!;

    private IdentidadeExternaUsuario() { }

    public IdentidadeExternaUsuario(Guid usuarioId, string emissor, string subject, DateTime? agoraUtc = null)
    {
        if (usuarioId == Guid.Empty) throw new ArgumentException("Usuário é obrigatório.", nameof(usuarioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(emissor);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        UsuarioId = usuarioId;
        Emissor = emissor.Trim().TrimEnd('/');
        Subject = subject.Trim();
        VinculadaEmUtc = agoraUtc ?? DateTime.UtcNow;
        UltimoLoginEmUtc = VinculadaEmUtc;
    }

    public void RegistrarLogin(DateTime? agoraUtc = null)
    {
        UltimoLoginEmUtc = agoraUtc ?? DateTime.UtcNow;
        AtualizarDataModificacao();
    }
}
