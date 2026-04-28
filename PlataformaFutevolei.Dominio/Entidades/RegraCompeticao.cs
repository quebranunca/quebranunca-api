namespace PlataformaFutevolei.Dominio.Entidades;

public class RegraCompeticao : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int PontosMinimosPartida { get; set; } = Competicao.PontosMinimosPartidaPadrao;
    public int DiferencaMinimaPartida { get; set; } = Competicao.DiferencaMinimaPartidaPadrao;
    public bool PermiteEmpate { get; set; } = Competicao.PermiteEmpatePadrao;
    public decimal PontosVitoria { get; set; } = Competicao.PontosVitoriaPadrao;
    public decimal PontosDerrota { get; set; } = Competicao.PontosDerrotaPadrao;
    public decimal PontosParticipacao { get; set; } = Competicao.PontosParticipacaoPadrao;
    public decimal PontosPrimeiroLugar { get; set; }
    public decimal PontosSegundoLugar { get; set; }
    public decimal PontosTerceiroLugar { get; set; }
    public Guid? UsuarioCriadorId { get; set; }

    public Usuario? UsuarioCriador { get; set; }
    public ICollection<Competicao> Competicoes { get; set; } = new List<Competicao>();
}
