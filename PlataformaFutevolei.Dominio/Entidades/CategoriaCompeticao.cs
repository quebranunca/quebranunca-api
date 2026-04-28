using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class CategoriaCompeticao : EntidadeBase
{
    public Guid CompeticaoId { get; set; }
    public Guid? FormatoCampeonatoId { get; set; }
    public Guid? TabelaJogosAprovadaPorUsuarioId { get; set; }
    public DateTime? TabelaJogosAprovadaEmUtc { get; set; }
    public string Nome { get; set; } = string.Empty;
    public GeneroCategoria Genero { get; set; }
    public NivelCategoria Nivel { get; set; }
    public decimal PesoRanking { get; set; } = 1m;
    public int? QuantidadeMaximaDuplas { get; set; }
    public bool InscricoesEncerradas { get; set; }

    public Competicao Competicao { get; set; } = default!;
    public FormatoCampeonato? FormatoCampeonato { get; set; }
    public ICollection<Partida> Partidas { get; set; } = new List<Partida>();
    public ICollection<InscricaoCampeonato> Inscricoes { get; set; } = new List<InscricaoCampeonato>();

    public bool TabelaJogosAprovada => TabelaJogosAprovadaEmUtc.HasValue;

    public FormatoCampeonato? ObterFormatoCampeonatoEfetivo()
    {
        return FormatoCampeonato ?? Competicao?.FormatoCampeonato;
    }

    public void AprovarTabelaJogos(Guid usuarioId, DateTime dataAprovacaoUtc)
    {
        TabelaJogosAprovadaPorUsuarioId = usuarioId;
        TabelaJogosAprovadaEmUtc = dataAprovacaoUtc;
        AtualizarDataModificacao();
    }

    public void LimparAprovacaoTabelaJogos()
    {
        TabelaJogosAprovadaPorUsuarioId = null;
        TabelaJogosAprovadaEmUtc = null;
        AtualizarDataModificacao();
    }

    public void EncerrarInscricoes()
    {
        InscricoesEncerradas = true;
        AtualizarDataModificacao();
    }

    public void ReabrirInscricoes()
    {
        InscricoesEncerradas = false;
        AtualizarDataModificacao();
    }
}
