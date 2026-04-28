using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public sealed record DefinicaoFormatoCampeonatoPadrao(
    string Nome,
    string? Descricao,
    TipoFormatoCampeonato TipoFormato,
    bool Ativo,
    int? QuantidadeGrupos,
    int? ClassificadosPorGrupo,
    bool GeraMataMataAposGrupos,
    bool TurnoEVolta,
    string? TipoChave,
    int? QuantidadeDerrotasParaEliminacao,
    bool PermiteCabecaDeChave,
    bool DisputaTerceiroLugar
);

public static class FormatosCampeonatoPadrao
{
    public const string NomePontosCorridos = "Formato padrão pontos corridos";
    public const string NomeFaseDeGrupos = "Formato padrão fase de grupos";
    public const string NomeChave = "Formato padrão chave";

    public static IReadOnlyList<DefinicaoFormatoCampeonatoPadrao> Lista { get; } =
    [
        new(
            NomePontosCorridos,
            "Formato padrão para grupos em pontos corridos.",
            TipoFormatoCampeonato.PontosCorridos,
            true,
            null,
            null,
            false,
            false,
            null,
            null,
            false,
            false
        ),
        new(
            NomeFaseDeGrupos,
            "Formato padrão de fase de grupos com mata-mata posterior.",
            TipoFormatoCampeonato.FaseDeGrupos,
            true,
            2,
            2,
            true,
            false,
            null,
            null,
            false,
            false
        ),
        new(
            NomeChave,
            "Formato padrão em chave com dupla eliminação.",
            TipoFormatoCampeonato.Chave,
            true,
            null,
            null,
            false,
            false,
            "Dupla eliminação",
            2,
            false,
            false
        )
    ];

    public static bool EhPadrao(string nome)
    {
        return Lista.Any(x => string.Equals(x.Nome, nome, StringComparison.Ordinal));
    }

    public static string? ObterNomePadraoPorTipo(TipoFormatoCampeonato tipoFormato)
    {
        return tipoFormato switch
        {
            TipoFormatoCampeonato.PontosCorridos => NomePontosCorridos,
            TipoFormatoCampeonato.FaseDeGrupos => NomeFaseDeGrupos,
            TipoFormatoCampeonato.Chave => NomeChave,
            _ => null
        };
    }
}
