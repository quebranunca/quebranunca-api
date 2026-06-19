using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class ExtratoPontuacaoBeneficio : EntidadeBase
{
    public Guid AtletaId { get; set; }
    public Guid? GrupoId { get; set; }
    public Guid? PartidaId { get; set; }
    public Guid? ResgateId { get; set; }
    public TipoEventoPontuacaoBeneficio TipoEvento { get; set; }
    public int Pontos { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string ChaveIdempotencia { get; set; } = string.Empty;
    public Guid? CriadoPorUsuarioId { get; set; }

    public Atleta Atleta { get; set; } = default!;
    public Grupo? Grupo { get; set; }
    public Partida? Partida { get; set; }
    public ResgateBeneficioPontuacao? Resgate { get; set; }
    public Usuario? CriadoPorUsuario { get; set; }
}
