using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record RankingFiltroInicialDto(
    string? TipoConsulta,
    Guid? CompeticaoId
);

public record RankingCategoriaDto(
    Guid CategoriaId,
    Guid CompeticaoId,
    string NomeCompeticao,
    string NomeCategoria,
    GeneroCategoria? Genero,
    IReadOnlyList<RankingAtletaDto> Atletas
);

public record RankingAtletaDto(
    int Posicao,
    Guid AtletaId,
    string NomeAtleta,
    string? ApelidoAtleta,
    string? Bairro,
    string? Cidade,
    string? Estado,
    LadoAtleta Lado,
    bool PossuiUsuarioVinculado,
    bool CadastroPendente,
    bool TemEmail,
    string StatusPendencia,
    int Jogos,
    int Vitorias,
    int Derrotas,
    int Empates,
    decimal Pontos,
    decimal PontosPendentes,
    IReadOnlyList<RankingPartidaDto> Partidas
);

public record RankingRegiaoFiltroDto(
    IReadOnlyList<string> Estados,
    IReadOnlyList<RankingRegiaoCidadeDto> Cidades,
    IReadOnlyList<RankingRegiaoBairroDto> Bairros
);

public record RankingRegiaoCidadeDto(
    string Estado,
    string Cidade
);

public record RankingRegiaoBairroDto(
    string Estado,
    string Cidade,
    string Bairro
);

public record RankingPartidaDto(
    Guid PartidaId,
    string Confronto,
    DateTime DataPartida,
    string NomeCompeticao,
    string NomeCategoria,
    string Resultado,
    decimal Pontos
);
