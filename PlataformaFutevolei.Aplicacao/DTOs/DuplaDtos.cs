namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarDuplaDto(
    string? Nome,
    Guid Atleta1Id,
    Guid Atleta2Id
);

public record AtualizarDuplaDto(
    string? Nome,
    Guid Atleta1Id,
    Guid Atleta2Id
);

public record DuplaDto(
    Guid Id,
    string Nome,
    Guid Atleta1Id,
    string NomeAtleta1,
    Guid Atleta2Id,
    string NomeAtleta2,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
