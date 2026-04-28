using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class InscricaoCampeonato : EntidadeBase
{
    public Guid CompeticaoId { get; set; }
    public Guid CategoriaCompeticaoId { get; set; }
    public Guid DuplaId { get; set; }
    public bool Pago { get; set; }
    public DateTime DataInscricaoUtc { get; set; } = DateTime.UtcNow;
    public StatusInscricaoCampeonato Status { get; set; } = StatusInscricaoCampeonato.PendenteAprovacao;
    public string? Observacao { get; set; }

    public Competicao Competicao { get; set; } = default!;
    public CategoriaCompeticao CategoriaCompeticao { get; set; } = default!;
    public Dupla Dupla { get; set; } = default!;
}
