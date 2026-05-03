using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class Competicao : EntidadeBase
{
    public const int PontosMinimosPartidaPadrao = 18;
    public const int DiferencaMinimaPartidaPadrao = 2;
    public const bool PermiteEmpatePadrao = false;
    public const decimal PontosVitoriaPadrao = 3m;
    public const decimal PontosDerrotaPadrao = 0m;
    public const decimal PontosParticipacaoPadrao = 0m;
    public const decimal PontosPrimeiroLugarPadrao = 0m;
    public const decimal PontosSegundoLugarPadrao = 0m;
    public const decimal PontosTerceiroLugarPadrao = 0m;

    public string Nome { get; set; } = string.Empty;
    public TipoCompeticao Tipo { get; set; }
    public string? Descricao { get; set; }
    public string? Link { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public Guid? LigaId { get; set; }
    public Guid? LocalId { get; set; }
    public Guid? FormatoCampeonatoId { get; set; }
    public Guid? RegraCompeticaoId { get; set; }
    public Guid? UsuarioOrganizadorId { get; set; }
    public bool ContaRankingLiga { get; set; }
    public bool InscricoesAbertas { get; set; }
    public bool PossuiFinalReset { get; set; }

    public Liga? Liga { get; set; }
    public Local? Local { get; set; }
    public FormatoCampeonato? FormatoCampeonato { get; set; }
    public RegraCompeticao? RegraCompeticao { get; set; }
    public Usuario? UsuarioOrganizador { get; set; }
    public ICollection<CategoriaCompeticao> Categorias { get; set; } = new List<CategoriaCompeticao>();
    public ICollection<InscricaoCampeonato> Inscricoes { get; set; } = new List<InscricaoCampeonato>();
    public int ObterPontosMinimosPartida() => RegraCompeticao?.PontosMinimosPartida ?? PontosMinimosPartidaPadrao;

    public int ObterDiferencaMinimaPartida() => RegraCompeticao?.DiferencaMinimaPartida ?? DiferencaMinimaPartidaPadrao;

    public bool ObterPermiteEmpate() => RegraCompeticao?.PermiteEmpate ?? PermiteEmpatePadrao;

    public decimal ObterPontosVitoria() => RegraCompeticao?.PontosVitoria ?? PontosVitoriaPadrao;

    public decimal ObterPontosDerrota() => RegraCompeticao?.PontosDerrota ?? PontosDerrotaPadrao;

    public decimal ObterPontosParticipacao() => RegraCompeticao?.PontosParticipacao ?? PontosParticipacaoPadrao;

    public decimal ObterPontosPrimeiroLugar() => RegraCompeticao?.PontosPrimeiroLugar ?? PontosPrimeiroLugarPadrao;

    public decimal ObterPontosSegundoLugar() => RegraCompeticao?.PontosSegundoLugar ?? PontosSegundoLugarPadrao;

    public decimal ObterPontosTerceiroLugar() => RegraCompeticao?.PontosTerceiroLugar ?? PontosTerceiroLugarPadrao;
}
