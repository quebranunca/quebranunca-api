using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class ResgateBeneficioPontuacao : EntidadeBase
{
    public Guid AtletaId { get; set; }
    public Guid BeneficioId { get; set; }
    public int PontosUtilizados { get; set; }
    public StatusResgateBeneficioPontuacao Status { get; set; } = StatusResgateBeneficioPontuacao.Solicitado;
    public string? CodigoCupom { get; set; }
    public string? ObservacaoAtleta { get; set; }
    public string? ObservacaoAdmin { get; set; }
    public DateTime SolicitadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AprovadoEm { get; set; }
    public DateTime? RejeitadoEm { get; set; }
    public DateTime? CanceladoEm { get; set; }
    public DateTime? UtilizadoEm { get; set; }
    public Guid? AtualizadoPorUsuarioId { get; set; }

    public Atleta Atleta { get; set; } = default!;
    public BeneficioPontuacao Beneficio { get; set; } = default!;
    public Usuario? AtualizadoPorUsuario { get; set; }
}
