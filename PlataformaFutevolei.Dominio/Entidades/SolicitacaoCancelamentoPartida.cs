using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class SolicitacaoCancelamentoPartida : EntidadeBase
{
    public Guid PartidaId { get; set; }
    public Guid SolicitadaPorUsuarioId { get; set; }
    public DateTime SolicitadaEm { get; set; } = DateTime.UtcNow;
    public Guid DuplaSolicitanteId { get; set; }
    public Guid DuplaAdversariaId { get; set; }
    public MotivoCancelamentoPartida Motivo { get; set; }
    public string? Observacao { get; set; }
    public StatusSolicitacaoCancelamentoPartida Status { get; set; } = StatusSolicitacaoCancelamentoPartida.Pendente;
    public Guid? RespondidaPorUsuarioId { get; set; }
    public DateTime? RespondidaEm { get; set; }
    public DateTime? CanceladaPeloSolicitanteEm { get; set; }

    public Partida Partida { get; set; } = default!;
    public Usuario SolicitadaPorUsuario { get; set; } = default!;
    public Usuario? RespondidaPorUsuario { get; set; }
    public Dupla DuplaSolicitante { get; set; } = default!;
    public Dupla DuplaAdversaria { get; set; } = default!;
    public ICollection<PendenciaUsuario> Pendencias { get; set; } = new List<PendenciaUsuario>();
}
