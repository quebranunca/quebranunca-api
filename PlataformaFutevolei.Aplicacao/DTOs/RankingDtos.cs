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
    string? FotoPerfilUrl,
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

public record RankingPaginaDto<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total,
    int TotalPaginas
);

public record RankingAtletaResumoDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string? FotoPerfilUrl
);

public record RankingSequenciaDto(
    string Tipo,
    int Quantidade,
    string Texto
);

public record RankingDuplaItemDto(
    string Id,
    int Posicao,
    RankingAtletaResumoDto Atleta1,
    RankingAtletaResumoDto Atleta2,
    int Jogos,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    RankingSequenciaDto SequenciaAtual,
    decimal PontosRanking,
    int Variacao,
    DateTime? UltimoJogo,
    string? GrupoPrincipal,
    int? PontosPro,
    int? PontosContra,
    int? Saldo
);

public record RankingDuplaJogoDto(
    Guid PartidaId,
    DateTime DataPartida,
    string Contexto,
    string DuplaAdversaria,
    string Resultado,
    string? Placar,
    bool PossuiPlacar
);

public record RankingDuplaAdversarioDto(
    string Id,
    string Nome,
    int Jogos,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    DateTime? UltimoConfronto,
    int? PontosPro,
    int? PontosContra,
    int? Saldo
);

public record RankingDuplaGrupoDto(
    Guid? GrupoId,
    string Nome,
    int Jogos,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    decimal PontosRanking,
    int? PontosPro,
    int? PontosContra,
    int? Saldo
);

public record RankingDuplaDetalheDto(
    RankingDuplaItemDto Resumo,
    IReadOnlyList<RankingDuplaJogoDto> UltimosJogos,
    IReadOnlyList<RankingDuplaAdversarioDto> PrincipaisAdversarios,
    IReadOnlyList<RankingDuplaGrupoDto> Grupos,
    IReadOnlyList<RankingDuplaJogoDto> Historico
);

public record RankingGrupoItemDto(
    Guid GrupoId,
    int Posicao,
    string Nome,
    string? FotoUrl,
    string? Cidade,
    int QuantidadeAtletas,
    int QuantidadePartidas,
    int AtletasAtivos,
    decimal PontuacaoRanking,
    int Variacao,
    DateTime? UltimaPartida
);

public record RankingGrupoJogoDto(
    Guid PartidaId,
    DateTime DataPartida,
    string DuplaA,
    string DuplaB,
    string Resultado,
    string? Placar,
    bool PossuiPlacar
);

public record RankingGrupoEvolucaoMensalDto(
    int Ano,
    int Mes,
    int Partidas,
    int AtletasAtivos,
    decimal PontuacaoRanking
);

public record RankingGrupoDetalheDto(
    Guid GrupoId,
    string Nome,
    string? FotoUrl,
    string? Cidade,
    string? Descricao,
    string? Administrador,
    bool Publico,
    int QuantidadeAtletas,
    int QuantidadePartidas,
    int AtletasAtivos,
    decimal PontuacaoRanking,
    IReadOnlyList<RankingAtletaDto> TopAtletas,
    IReadOnlyList<RankingDuplaItemDto> TopDuplas,
    IReadOnlyList<RankingGrupoJogoDto> UltimosJogos,
    IReadOnlyList<RankingGrupoEvolucaoMensalDto> EvolucaoMensal
);
