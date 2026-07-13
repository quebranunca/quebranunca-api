namespace PlataformaFutevolei.Dominio.Entidades;

public class HistoricoPartida : EntidadeBase
{
    public Guid PartidaIdOriginal { get; set; }
    public string Acao { get; set; } = null!;
    public Guid UsuarioResponsavelId { get; set; }
    public DateTime DataHoraUtc { get; set; } = DateTime.UtcNow;
    public string? Motivo { get; set; }
    public string SnapshotJson { get; set; } = null!;
    public string? CorrelationId { get; set; }
}
