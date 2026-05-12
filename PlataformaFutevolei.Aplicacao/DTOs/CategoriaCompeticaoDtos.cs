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
    Guid CategoriaId,
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
    StatusInscricoesCategoriaCampeonato StatusInscricao,
    decimal ValorInscricao,
    DateTime? DataAberturaInscricao,
    DateTime? DataEncerramentoInscricao,
    bool PermiteListaEspera,
    string? Observacao,
    bool Ativo,
    int QuantidadeDuplasInscritas,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record CategoriaDisponivelVinculoDto(
    Guid Id,
    string Nome,
    GeneroCategoria Genero,
    NivelCategoria Nivel
);
