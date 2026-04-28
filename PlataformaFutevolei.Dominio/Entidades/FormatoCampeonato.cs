using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class FormatoCampeonato : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public TipoFormatoCampeonato TipoFormato { get; set; }
    public bool Ativo { get; set; } = true;

    public int? QuantidadeGrupos { get; set; }
    public int? ClassificadosPorGrupo { get; set; }
    public bool GeraMataMataAposGrupos { get; set; }
    public bool TurnoEVolta { get; set; }

    public string? TipoChave { get; set; }
    public int? QuantidadeDerrotasParaEliminacao { get; set; }
    public bool PermiteCabecaDeChave { get; set; }
    public bool DisputaTerceiroLugar { get; set; }
}
