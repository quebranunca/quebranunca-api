using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public sealed record DefinicaoRegraCompeticaoPadrao(
    string Nome,
    string? Descricao,
    int PontosMinimosPartida,
    int DiferencaMinimaPartida,
    bool PermiteEmpate,
    decimal PontosVitoria,
    decimal PontosDerrota,
    decimal PontosParticipacao,
    decimal PontosPrimeiroLugar,
    decimal PontosSegundoLugar,
    decimal PontosTerceiroLugar
);

public static class RegrasCompeticaoPadrao
{
    public static IReadOnlyList<DefinicaoRegraCompeticaoPadrao> Lista { get; } =
    [
        new(
            "Regra padrão",
            "Regra base com o fallback oficial do sistema.",
            Competicao.PontosMinimosPartidaPadrao,
            Competicao.DiferencaMinimaPartidaPadrao,
            Competicao.PermiteEmpatePadrao,
            Competicao.PontosVitoriaPadrao,
            Competicao.PontosDerrotaPadrao,
            Competicao.PontosParticipacaoPadrao,
            Competicao.PontosPrimeiroLugarPadrao,
            Competicao.PontosSegundoLugarPadrao,
            Competicao.PontosTerceiroLugarPadrao
        ),
        new(
            "Regra padrão campeonato",
            "Regra base da temporada para campeonatos.",
            Competicao.PontosMinimosPartidaPadrao,
            Competicao.DiferencaMinimaPartidaPadrao,
            Competicao.PermiteEmpatePadrao,
            Competicao.PontosVitoriaPadrao,
            Competicao.PontosDerrotaPadrao,
            Competicao.PontosParticipacaoPadrao,
            15m,
            9m,
            6m
        )
    ];

    public static bool EhPadrao(string nome)
    {
        return Lista.Any(x => string.Equals(x.Nome, nome, StringComparison.Ordinal));
    }
}
