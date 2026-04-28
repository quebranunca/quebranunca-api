namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarRegraCompeticaoDto(
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

public record AtualizarRegraCompeticaoDto(
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

public record RegraCompeticaoDto(
    Guid Id,
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
    decimal PontosTerceiroLugar,
    bool EhPadrao,
    Guid? UsuarioCriadorId,
    string? NomeUsuarioCriador,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
