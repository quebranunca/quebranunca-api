using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarCategoriaCompeticaoDto(
    Guid CompeticaoId,
    Guid? FormatoCampeonatoId,
    string Nome,
    GeneroCategoria Genero,
    NivelCategoria Nivel,
    decimal? PesoRanking,
    int? QuantidadeMaximaDuplas,
    bool InscricoesEncerradas
);

public record AtualizarCategoriaCompeticaoDto(
    Guid? FormatoCampeonatoId,
    string Nome,
    GeneroCategoria Genero,
    NivelCategoria Nivel,
    decimal? PesoRanking,
    int? QuantidadeMaximaDuplas,
    bool InscricoesEncerradas
);

public record CategoriaCompeticaoDto(
    Guid Id,
    Guid CompeticaoId,
    Guid? FormatoCampeonatoId,
    Guid? FormatoCampeonatoEfetivoId,
    bool TabelaJogosAprovada,
    Guid? TabelaJogosAprovadaPorUsuarioId,
    DateTime? TabelaJogosAprovadaEmUtc,
    string NomeCompeticao,
    string? NomeFormatoCampeonato,
    string? NomeFormatoCampeonatoEfetivo,
    bool UsaFormatoCampeonatoDaCompeticao,
    string Nome,
    GeneroCategoria Genero,
    NivelCategoria Nivel,
    decimal PesoRanking,
    int? QuantidadeMaximaDuplas,
    bool InscricoesEncerradas,
    int QuantidadeDuplasInscritas,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
